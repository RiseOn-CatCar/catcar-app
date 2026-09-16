variable "location" {
  description = "Azure region for CatCar resources."
  type        = string
  default     = "eastus2"
}

variable "environment" {
  description = "Deployment environment name."
  type        = string
  default     = "prod"
}

variable "resource_group_name" {
  description = "Name of the Azure resource group."
  type        = string
  default     = "rg-catcar-prod"
}

variable "domain_name" {
  description = "DNS-safe application identifier used in Azure resource names."
  type        = string
  default     = "catcar"

  validation {
    condition     = can(regex("^[a-z0-9-]+$", var.domain_name))
    error_message = "domain_name must contain only lowercase letters, numbers, and hyphens."
  }
}

variable "postgres_admin_user" {
  description = "Administrator login for the PostgreSQL Flexible Server."
  type        = string
  default     = "catcaradmin"
}

variable "postgres_admin_password" {
  description = "Administrator password for the PostgreSQL Flexible Server."
  type        = string
  sensitive   = true

  validation {
    condition     = length(var.postgres_admin_password) >= 12
    error_message = "postgres_admin_password must be at least 12 characters long."
  }
}

variable "jwt_secret" {
  description = "Signing secret for JWT tokens; supply a cryptographically random value of at least 32 characters."
  type        = string
  sensitive   = true

  validation {
    condition     = length(var.jwt_secret) >= 32
    error_message = "jwt_secret must be at least 32 characters long."
  }
}

variable "aks_node_count" {
  description = "Initial number of AKS system nodes."
  type        = number
  default     = 3

  validation {
    condition     = var.aks_node_count >= 1
    error_message = "aks_node_count must be at least one."
  }
}

variable "aks_vm_size" {
  description = "VM size for the AKS system node pool."
  type        = string
  default     = "Standard_D2s_v3"
}
