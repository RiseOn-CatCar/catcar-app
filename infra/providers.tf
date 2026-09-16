terraform {
  required_version = ">= 1.5.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
    azapi = {
      source  = "Azure/azapi"
      version = "~> 2.0"
    }
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 2.0"
    }
  }

  backend "azurerm" {}
}

provider "azurerm" {
  features {}
}

provider "azapi" {}

provider "kubernetes" {
  host                   = azurerm_kubernetes_cluster.catcar.kube_config[0].host
  client_certificate     = base64decode(azurerm_kubernetes_cluster.catcar.kube_config[0].client_certificate)
  client_key             = base64decode(azurerm_kubernetes_cluster.catcar.kube_config[0].client_key)
  cluster_ca_certificate = base64decode(azurerm_kubernetes_cluster.catcar.kube_config[0].cluster_ca_certificate)
}
