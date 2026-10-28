resource "aws_cognito_user_pool" "backend" {
  name = "restaurant-backend-user-pool"

  deletion_protection = "INACTIVE"

  password_policy {
    minimum_length                   = 8
    require_lowercase                = true
    require_numbers                  = true
    require_symbols                  = true
    require_uppercase                = true
    temporary_password_validity_days = 7
  }

  account_recovery_setting {
    recovery_mechanism {
      name     = "verified_email"
      priority = 1
    }
    recovery_mechanism {
      name     = "verified_phone_number"
      priority = 2
    }
  }

  admin_create_user_config {
    allow_admin_create_user_only = false
  }
}

resource "aws_cognito_user_pool_client" "backend_api" {
  name         = "restaurant-backend-api-client"
  user_pool_id = aws_cognito_user_pool.backend.id

  generate_secret = false

  explicit_auth_flows = ["ALLOW_USER_PASSWORD_AUTH"]

  refresh_token_validity = 30
  auth_session_validity  = 3

  enable_token_revocation = true

  # Cognito's API never returns generate_secret, so after `terraform import`
  # it stays unset in state and any explicit value here reads as a forced
  # replacement. Ignore it — the client was created with no secret.
  lifecycle {
    ignore_changes = [generate_secret]
  }
}
