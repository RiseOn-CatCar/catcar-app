output "resource_group_name" {
  description = "Name of the resource group containing CatCar resources."
  value       = azurerm_resource_group.catcar.name
}

output "acr_login_server" {
  description = "Login server for the CatCar Azure Container Registry."
  value       = azurerm_container_registry.catcar.login_server
}

output "aks_cluster_name" {
  description = "Name of the CatCar AKS cluster."
  value       = azurerm_kubernetes_cluster.catcar.name
}

output "postgresql_fqdn" {
  description = "Fully qualified domain name of the PostgreSQL Flexible Server."
  value       = azurerm_postgresql_flexible_server.catcar.fqdn
}

output "key_vault_uri" {
  description = "URI of the Key Vault containing CatCar deployment secrets."
  value       = azurerm_key_vault.catcar.vault_uri
}

output "key_vault_name" {
  description = "Name of the Key Vault used by the deployment workflow."
  value       = azurerm_key_vault.catcar.name
}
