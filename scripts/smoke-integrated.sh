#!/usr/bin/env bash
set -euo pipefail
set +x
umask 077

if [[ $# != 2 || $1 != --mode || ( $2 != kind && $2 != azure ) ]]; then
  echo 'Usage: smoke-integrated.sh --mode kind|azure' >&2
  exit 2
fi
mode=$2
: "${API_BASE_URL:?}" "${AUTH_BASE_URL:?}" "${HEALTH_BASE_URL:?}" \
  "${BOOTSTRAP_ADMIN_EMAIL:?}" "${BOOTSTRAP_ADMIN_PASSWORD:?}" "${SMOKE_RESULT_FILE:?}"
if [[ $mode == kind ]]; then
  : "${MAILPIT_BASE_URL:?}"
else
  : "${SMOKE_CUSTOMER_EMAIL:?}"
fi
api=${API_BASE_URL%/}
auth=${AUTH_BASE_URL%/}
health=${HEALTH_BASE_URL%/}
so="$api/api/v1/service-operations"
ci="$api/api/v1/catalog-inventory"
communication="$api/api/v1/communication/approval-links"

checks='{"health":false,"admin_login":false,"customer_auth":false,"invalid_and_inactive_customer":false,"cross_customer_isolation":false,"invalid_tokens":false}'
if [[ $mode == kind ]]; then
  checks=$(jq -c '. + {"issue_budget":false,"approve_and_replay":false,"reject_budget":false,"insufficient_stock":false,"complete_and_deliver":false,"no_double_stock_deduction":false}' <<<"$checks")
fi
save_results() {
  local code=$?
  trap - EXIT
  if ! printf '%s\n' "$checks" > "$SMOKE_RESULT_FILE"; then
    echo 'Could not write smoke results' >&2
    exit 1
  fi
  exit "$code"
}
trap save_results EXIT
pass() { checks=$(jq -c --arg name "$1" '.[$name] = true' <<<"$checks"); }
fail() { printf 'Smoke failed: %s\n' "$1" >&2; exit 1; }

# Responses and credentials stay in shell memory; do not print response bodies or URLs.
request() {
  local expected=$1 method=$2 url=$3 token=${4:-} data=${5:-} response status
  local -a args=(-s --connect-timeout 5 --max-time 30 -w $'\n%{http_code}' -X "$method" -H 'Content-Type: application/json')
  [[ -z $token ]] || args+=(-H "Authorization: Bearer $token")
  [[ -z $data ]] || args+=(--data-binary "$data")
  response=$(curl "${args[@]}" "$url") || fail "HTTP transport error ($method)"
  status=${response##*$'\n'}
  body=${response%$'\n'*}
  [[ $status == "$expected" ]] || fail "HTTP $status, expected $expected ($method)"
}
cpf() {
  local digits d sum rem i
  digits=$(od -An -N4 -tu4 /dev/urandom | tr -d ' ')
  printf -v digits '%09d' "$((digits % 1000000000))"
  for d in 10 11; do
    sum=0
    for ((i=0;i<d-1;i++)); do sum=$((sum + 10#${digits:i:1} * (d-i))); done
    rem=$((sum * 10 % 11)); ((rem == 10)) && rem=0
    digits+="$rem"
  done
  printf '%s' "$digits"
}
request 200 GET "$health/health/ready"
pass health
request 200 POST "$api/api/v1/identity-access/auth/login" '' "$(jq -nc --arg email "$BOOTSTRAP_ADMIN_EMAIL" --arg password "$BOOTSTRAP_ADMIN_PASSWORD" '{email:$email,password:$password}')"
admin=$(jq -er '.accessToken | strings | select(length > 0)' <<<"$body")
pass admin_login
suffix=$(od -An -N6 -tx1 /dev/urandom | tr -d ' \n')
if [[ $mode == kind ]]; then
  email_a="smoke-a-$suffix@catcar.local"
  email_b="smoke-b-$suffix@catcar.local"
  email_c="smoke-c-$suffix@catcar.local"
else
  email_a=$SMOKE_CUSTOMER_EMAIL
  email_b=$SMOKE_CUSTOMER_EMAIL
  email_c=$SMOKE_CUSTOMER_EMAIL
fi
create_customer() {
  request 201 POST "$so/customers" "$admin" "$(jq -nc --arg document "$1" --arg name "CatCar Smoke $2 $suffix" --arg email "$3" '{document:$document,name:$name,phone:"11999999999",email:$email}')"
}
a=$(cpf); b=$(cpf); inactive=$(cpf); missing=$(cpf)
create_customer "$a" A "$email_a"; aid=$(jq -er '.id' <<<"$body")
create_customer "$b" B "$email_b"; bid=$(jq -er '.id' <<<"$body")
request 404 POST "$auth/api/auth/customer" '' "$(jq -nc --arg d "$missing" '{documentNumber:$d}')"
create_customer "$inactive" Inactive "$email_c"; iid=$(jq -er '.id' <<<"$body")
request 200 PATCH "$so/customers/$iid/status" "$admin" '{"isActive":false}'
request 403 POST "$auth/api/auth/customer" '' "$(jq -nc --arg d "$inactive" '{documentNumber:$d}')"
request 400 POST "$auth/api/auth/customer" '' '{"documentNumber":"00000000000"}'
pass invalid_and_inactive_customer
request 200 POST "$auth/api/auth/customer" '' "$(jq -nc --arg d "$a" '{documentNumber:$d}')"
token_a=$(jq -er '.token' <<<"$body")
claims=${token_a#*.}; claims=${claims%%.*}; claims=${claims//-/+}; claims=${claims//_/\/}
while ((${#claims} % 4)); do claims+='='; done
printf '%s' "$claims" | base64 -d | jq -e --arg id "$aid" '.iss == "CatCar" and (.aud == "CatCar.Api" or (.aud | arrays | index("CatCar.Api") != null)) and .role == "Customer" and .customer_id == $id' >/dev/null
request 200 POST "$auth/api/auth/customer" '' "$(jq -nc --arg d "$b" '{documentNumber:$d}')"
token_b=$(jq -er '.token' <<<"$body")
pass customer_auth
plate="CAT$(printf '%04d' "$((16#${suffix:0:6} % 10000))")"
request 201 POST "$so/vehicles" "$admin" "$(jq -nc --arg id "$aid" --arg plate "$plate" '{customerId:$id,plate:$plate,brand:"CatCar Smoke",model:"Smoke",manufactureYear:2020}')"
vehicle=$(jq -er '.id' <<<"$body")
order=$(jq -nc --arg id "$aid" --arg vehicle "$vehicle" '{customerId:$id,vehicleId:$vehicle,initialDescription:"CatCar Smoke",services:[],parts:[]}')
request 201 POST "$so/work-orders" "$token_a" "$order"
wid=$(jq -er '.workOrderId' <<<"$body")
request 403 POST "$so/work-orders" "$token_a" "$(jq -nc --arg id "$bid" --arg vehicle "$vehicle" '{customerId:$id,vehicleId:$vehicle,initialDescription:"CatCar Smoke",services:[],parts:[]}')"
request 404 GET "$so/work-orders/$wid/progress" "$token_b"
jq -e '.detail == "Work order not found."' <<<"$body" >/dev/null
pass cross_customer_isolation
request 401 GET "$so/work-orders/$wid/progress" "${token_a%.*}.invalid"
request 404 GET "$communication/not-a-valid-token"
pass invalid_tokens

if [[ $mode == kind ]]; then
  mailpit=${MAILPIT_BASE_URL%/}
  # Each recipient belongs to one newly created customer, so old mail cannot be mistaken for this run.
  approval_token() {
    local recipient=$1 messages id message token attempt
    for ((attempt=0; attempt<90; attempt++)); do
      messages=$(curl -s --connect-timeout 5 --max-time 10 "$mailpit/api/v1/messages") || fail 'Mailpit transport error'
      id=$(jq -r --arg recipient "$recipient" '[.messages[]? | select(any(.To[]?; .Address == $recipient)) | select(.Subject == "CatCar - Orçamento aguardando sua aprovação") | .ID][0] // empty' <<<"$messages")
      if [[ -n $id ]]; then
        message=$(curl -s --connect-timeout 5 --max-time 10 "$mailpit/api/v1/message/$id") || fail 'Mailpit message transport error'
        token=$(jq -r '.HTML | capture("/api/v1/communication/approval-links/(?<token>[A-Za-z0-9_-]+)").token // empty' <<<"$message")
        [[ -n $token ]] || fail 'Approval email did not contain a token'
        printf '%s' "$token"
        return
      fi
      sleep 2
    done
    fail 'Timed out waiting for approval email'
  }
  wait_status() {
    local order_id=$1 customer_token=$2 expected=$3 attempt
    for ((attempt=0; attempt<90; attempt++)); do
      request 200 GET "$so/work-orders/$order_id/progress" "$customer_token"
      if jq -e --arg status "$expected" '.status == $status' <<<"$body" >/dev/null; then return; fi
      sleep 2
    done
    fail "Timed out waiting for work order status $expected"
  }
  request 201 POST "$ci/inventory-items" "$admin" "$(jq -nc --arg sku "SMOKE-$suffix" '{sku:$sku,name:"CatCar Smoke part",description:"CatCar Smoke",unitPrice:10,initialQuantity:2,minimumStockThreshold:0}')"
  item=$(jq -er '.id' <<<"$body")
  request 200 POST "$so/work-orders/$wid/parts" "$admin" "$(jq -nc --arg id "$item" '{inventoryItemId:$id,quantity:1}')"
  request 201 POST "$so/work-orders/$wid/budget" "$admin"
  budget=$(jq -er '.budgetId' <<<"$body")
  pass issue_budget
  link=$(approval_token "$email_a")
  request 200 GET "$communication/$link"
  jq -e --arg budget "$budget" '.budgetId == $budget' <<<"$body" >/dev/null
  request 200 POST "$communication/$link/decision" '' '{"approved":true}'
  jq -e --arg budget "$budget" '.budgetId == $budget and .budgetStatus == "Approved"' <<<"$body" >/dev/null
  request 400 POST "$communication/$link/decision" '' '{"approved":true}'
  pass approve_and_replay
  wait_status "$wid" "$token_a" InExecution
  request 200 GET "$ci/inventory-items/$item" "$admin"
  jq -e '.quantityInStock == 1' <<<"$body" >/dev/null
  request 200 POST "$so/work-orders/$wid/complete" "$admin"
  request 200 POST "$so/work-orders/$wid/deliver" "$admin"
  wait_status "$wid" "$token_a" Delivered
  pass complete_and_deliver
  request 400 POST "$so/work-orders/$wid/complete" "$admin"
  request 400 POST "$so/work-orders/$wid/deliver" "$admin"
  request 200 GET "$ci/inventory-items/$item" "$admin"
  jq -e '.quantityInStock == 1' <<<"$body" >/dev/null
  pass no_double_stock_deduction

  # A different recipient ensures that the second link cannot be confused with the first.
  request 201 POST "$so/vehicles" "$admin" "$(jq -nc --arg id "$bid" --arg plate "CAB${plate:3}" '{customerId:$id,plate:$plate,brand:"CatCar Smoke",model:"Smoke",manufactureYear:2020}')"
  vehicle_b=$(jq -er '.id' <<<"$body")
  order_b=$(jq -nc --arg id "$bid" --arg vehicle "$vehicle_b" --arg item "$item" '{customerId:$id,vehicleId:$vehicle,initialDescription:"CatCar Smoke rejection",services:[],parts:[{id:$item,quantity:1}]}')
  request 201 POST "$so/work-orders" "$token_b" "$order_b"
  wid_b=$(jq -er '.workOrderId' <<<"$body")
  link_b=$(approval_token "$email_b")
  request 200 POST "$communication/$link_b/decision" '' '{"approved":false,"reason":"Smoke rejection"}'
  jq -e '.budgetStatus == "Rejected" and .workOrderStatus == "InDiagnosis"' <<<"$body" >/dev/null
  request 400 POST "$communication/$link_b/decision" '' '{"approved":false,"reason":"Smoke rejection"}'
  pass reject_budget

  # A third customer isolates the insufficient-stock approval email.
  c=$(cpf)
  create_customer "$c" C "$email_c"; cid=$(jq -er '.id' <<<"$body")
  request 200 POST "$auth/api/auth/customer" '' "$(jq -nc --arg d "$c" '{documentNumber:$d}')"
  token_c=$(jq -er '.token' <<<"$body")
  request 201 POST "$so/vehicles" "$admin" "$(jq -nc --arg id "$cid" --arg plate "CAC${plate:3}" '{customerId:$id,plate:$plate,brand:"CatCar Smoke",model:"Smoke",manufactureYear:2020}')"
  vehicle_c=$(jq -er '.id' <<<"$body")
  request 201 POST "$so/work-orders" "$token_c" "$(jq -nc --arg id "$cid" --arg vehicle "$vehicle_c" --arg item "$item" '{customerId:$id,vehicleId:$vehicle,initialDescription:"CatCar Smoke insufficient stock",services:[],parts:[{id:$item,quantity:2}]}')"
  wid_c=$(jq -er '.workOrderId' <<<"$body")
  link_c=$(approval_token "$email_c")
  request 200 POST "$communication/$link_c/decision" '' '{"approved":true}'
  wait_status "$wid_c" "$token_c" InDiagnosis
  request 200 GET "$ci/inventory-items/$item" "$admin"
  jq -e '.quantityInStock == 1' <<<"$body" >/dev/null
  request 400 POST "$so/work-orders/$wid_c/complete" "$admin"
  pass insufficient_stock
fi
echo "Integrated $mode smoke passed"
