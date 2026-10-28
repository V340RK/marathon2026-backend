# Target lambda is managed by Syndicate, not by this Terraform config — name never
# changes (fixed by the backend directory structure), so the ARN is derived, not hardcoded.
locals {
  report_sender_lambda_arn = "arn:aws:lambda:${var.aws_region}:${var.account_id}:function:report-sender"

  # Names below are fixed by Syndicate deployment configs (lambda_config.json /
  # syndicate_aliases.yml), not managed by this Terraform config.
  api_handler_name     = "api-handler"
  reports_handler_name = "reports-handler"
  report_sender_name   = "report-sender"
  api_gateway_name     = "restaurant-backend-api"
}
