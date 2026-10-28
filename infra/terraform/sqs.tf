resource "aws_sqs_queue" "reports_dlq" {
  name                      = "restaurant-reports-queue-dlq"
  message_retention_seconds = 1209600
  sqs_managed_sse_enabled   = true
}

resource "aws_sqs_queue" "reports" {
  name                       = "restaurant-reports-queue"
  delay_seconds              = 0
  max_message_size           = 1024
  message_retention_seconds  = 345600
  receive_wait_time_seconds  = 0
  visibility_timeout_seconds = 600
  sqs_managed_sse_enabled    = true

  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.reports_dlq.arn
    maxReceiveCount     = 3
  })
}
