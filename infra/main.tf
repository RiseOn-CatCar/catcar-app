data "azurerm_client_config" "current" {}

locals {
  resource_prefix = "${var.domain_name}-${var.environment}"
  acr_name        = lower(replace("${var.domain_name}${var.environment}", "/[^0-9a-z]/", ""))
}

resource "azurerm_resource_group" "catcar" {
  name     = var.resource_group_name
  location = var.location

  tags = {
    application = var.domain_name
    environment = var.environment
    managed_by  = "terraform"
  }
}

resource "azurerm_virtual_network" "catcar" {
  name                = "vnet-${local.resource_prefix}"
  location            = azurerm_resource_group.catcar.location
  resource_group_name = azurerm_resource_group.catcar.name
  address_space       = ["10.20.0.0/16"]
}

resource "azurerm_subnet" "aks" {
  name                 = "snet-aks"
  resource_group_name  = azurerm_resource_group.catcar.name
  virtual_network_name = azurerm_virtual_network.catcar.name
  address_prefixes     = ["10.20.1.0/24"]
}

resource "azurerm_subnet" "postgresql" {
  name                 = "snet-postgresql"
  resource_group_name  = azurerm_resource_group.catcar.name
  virtual_network_name = azurerm_virtual_network.catcar.name
  address_prefixes     = ["10.20.2.0/24"]

  delegation {
    name = "postgresql-flexible-server"

    service_delegation {
      name = "Microsoft.DBforPostgreSQL/flexibleServers"
      actions = [
        "Microsoft.Network/virtualNetworks/subnets/join/action",
      ]
    }
  }
}

resource "azurerm_subnet" "private_endpoints" {
  name                 = "snet-private-endpoints"
  resource_group_name  = azurerm_resource_group.catcar.name
  virtual_network_name = azurerm_virtual_network.catcar.name
  address_prefixes     = ["10.20.3.0/24"]
}

resource "azurerm_private_dns_zone" "postgresql" {
  name                = "private.postgres.database.azure.com"
  resource_group_name = azurerm_resource_group.catcar.name
}

resource "azurerm_private_dns_zone_virtual_network_link" "postgresql" {
  name                  = "pdnslink-postgresql-${local.resource_prefix}"
  private_dns_zone_name = azurerm_private_dns_zone.postgresql.name
  virtual_network_id    = azurerm_virtual_network.catcar.id
  resource_group_name   = azurerm_resource_group.catcar.name
}

resource "azurerm_private_dns_zone" "acr" {
  name                = "privatelink.azurecr.io"
  resource_group_name = azurerm_resource_group.catcar.name
}

resource "azurerm_private_dns_zone_virtual_network_link" "acr" {
  name                  = "pdnslink-acr-${local.resource_prefix}"
  private_dns_zone_name = azurerm_private_dns_zone.acr.name
  virtual_network_id    = azurerm_virtual_network.catcar.id
  resource_group_name   = azurerm_resource_group.catcar.name
}

resource "azurerm_container_registry" "catcar" {
  name                          = local.acr_name
  resource_group_name           = azurerm_resource_group.catcar.name
  location                      = azurerm_resource_group.catcar.location
  sku                           = "Premium"
  admin_enabled                 = false
  public_network_access_enabled = false

  identity {
    type = "SystemAssigned"
  }
}

resource "azurerm_private_endpoint" "acr" {
  name                = "pe-acr-${local.resource_prefix}"
  location            = azurerm_resource_group.catcar.location
  resource_group_name = azurerm_resource_group.catcar.name
  subnet_id           = azurerm_subnet.private_endpoints.id

  private_service_connection {
    name                           = "psc-acr-${local.resource_prefix}"
    private_connection_resource_id = azurerm_container_registry.catcar.id
    subresource_names              = ["registry"]
    is_manual_connection           = false
  }

  private_dns_zone_group {
    name                 = "acr-private-dns"
    private_dns_zone_ids = [azurerm_private_dns_zone.acr.id]
  }

  depends_on = [azurerm_private_dns_zone_virtual_network_link.acr]
}

resource "azurerm_kubernetes_cluster" "catcar" {
  name                = "aks-${local.resource_prefix}"
  location            = azurerm_resource_group.catcar.location
  resource_group_name = azurerm_resource_group.catcar.name
  dns_prefix          = local.resource_prefix
  sku_tier            = "Standard"

  default_node_pool {
    name                 = "system"
    node_count           = var.aks_node_count
    vm_size              = var.aks_vm_size
    vnet_subnet_id       = azurerm_subnet.aks.id
    auto_scaling_enabled = false
    type                 = "VirtualMachineScaleSets"
  }

  identity {
    type = "SystemAssigned"
  }

  network_profile {
    network_plugin    = "azure"
    network_policy    = "cilium"
    load_balancer_sku = "standard"
  }

  role_based_access_control_enabled = true
  oidc_issuer_enabled               = true
  workload_identity_enabled         = true

  depends_on = [azurerm_subnet.aks]
}

resource "azurerm_role_assignment" "aks_acr_pull" {
  scope                            = azurerm_container_registry.catcar.id
  role_definition_name             = "AcrPull"
  principal_id                     = azurerm_kubernetes_cluster.catcar.kubelet_identity[0].object_id
  skip_service_principal_aad_check = true
}

resource "azurerm_postgresql_flexible_server" "catcar" {
  name                   = "psql-${local.resource_prefix}"
  resource_group_name    = azurerm_resource_group.catcar.name
  location               = azurerm_resource_group.catcar.location
  version                = "17"
  administrator_login    = var.postgres_admin_user
  administrator_password = var.postgres_admin_password
  sku_name               = "GP_Standard_D2s_v3"
  storage_mb             = 32768
  backup_retention_days  = 7

  delegated_subnet_id           = azurerm_subnet.postgresql.id
  private_dns_zone_id           = azurerm_private_dns_zone.postgresql.id
  public_network_access_enabled = false

  depends_on = [azurerm_private_dns_zone_virtual_network_link.postgresql]
}
resource "azurerm_postgresql_flexible_server_database" "catcar" {
  name      = "catcar"
  server_id = azurerm_postgresql_flexible_server.catcar.id
  charset   = "UTF8"
  collation = "en_US.utf8"
}


resource "azurerm_key_vault" "catcar" {
  name                       = "kv-${local.resource_prefix}"
  location                   = azurerm_resource_group.catcar.location
  resource_group_name        = azurerm_resource_group.catcar.name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  rbac_authorization_enabled = true
  purge_protection_enabled   = true
  soft_delete_retention_days = 90
}

resource "azurerm_role_assignment" "terraform_key_vault_secrets_officer" {
  scope                = azurerm_key_vault.catcar.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = data.azurerm_client_config.current.object_id
}

resource "azurerm_key_vault_secret" "postgres_connection_string" {
  name         = "postgres-connection-string"
  value        = "Host=${azurerm_postgresql_flexible_server.catcar.fqdn};Port=5432;Database=${azurerm_postgresql_flexible_server_database.catcar.name};Username=${var.postgres_admin_user};Password=${var.postgres_admin_password};Ssl Mode=Require;Trust Server Certificate=false"
  key_vault_id = azurerm_key_vault.catcar.id

  depends_on = [azurerm_role_assignment.terraform_key_vault_secrets_officer]
}

resource "azurerm_key_vault_secret" "jwt_secret" {
  name         = "jwt-secret"
  value        = var.jwt_secret
  key_vault_id = azurerm_key_vault.catcar.id

  depends_on = [azurerm_role_assignment.terraform_key_vault_secrets_officer]
}
