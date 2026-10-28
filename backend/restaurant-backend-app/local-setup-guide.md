# Local Setup Guide

This document explains how to prepare your local environment and AWS account for deploying the restaurant backend application.

---

## 1. AWS Prerequisites

Before deploying the project, make sure you have an AWS account and a user with permissions to manage the required resources.

### 1.1 Create an AWS account

Create or use an AWS account for development.

- AWS account: `https://us-east-1.console.aws.amazon.com`

### 1.2 Create an IAM user

In the AWS:

1. Open **IAM**
2. Create a new user for local development and deployment
3. Generate an **Access Key**
   - Use case: **Command Line Interface (CLI)**
4. Attach the required permissions
   - For local learning/dev setup, this project currently expects:
     - `AdministratorAccess`

> Note: `AdministratorAccess` is convenient for development, but it is broader than what is recommended for production use.

### 1.3 Create the deployment artifacts bucket

Create an S3 bucket that will be used for deployment artifacts.

Make sure the bucket name is globally unique in AWS. If it already exists, choose a similar unique name.

---

## 2. Local AWS CLI Setup

Authenticate your local machine with AWS using the IAM access key you created.

Run:

```powershell
aws configure
```

When prompted, provide:

- **AWS Access Key ID**
- **AWS Secret Access Key**
- **Default region** (for example: `us-east-1`)

---

## 3. Syndicate Configuration

This project uses **AWS Syndicate** for packaging and deployment.

Before running deployment commands, update the configuration files.

### 3.1 Update `syndicate.yml`

Open `syndicate.yml` and verify the following values:

1. `account_id` — set this to your AWS account ID
2. `project_path` — set this to the correct local path for the project
2. `deploy_target_bucket` - set this to your S3 bucket

### 3.2 Update `syndicate_aliases.yml`

Open `syndicate_aliases.yml` and verify or replace the following:

1. `account_id` — your AWS account ID
2. `imagesBucketName` - your S3 bucket name for images
3. `profileImagesBucketName` - your S3 bucket name for profile images
4. `swagger_ui_hoster_bucket_name` - your S3 bucket name for Swagger UI hosting

> Important: S3 bucket names are globally unique across all AWS accounts.  
> Use unique values for:
>
> - `imagesBucketName`
> - `profileImagesBucketName`
> - `swagger_ui_hoster_bucket_name`
>
> Example:
>
> ```yml
> imagesBucketName: "restaurant-backend-dev-images-yourname"
> profileImagesBucketName: "restaurant-backend-dev-profile-images-yourname"
> swagger_ui_hoster_bucket_name: "restaurant-backend-dev-swagger-ui-yourname"
> ```

If multiple users deploy this project, each user should use their own unique bucket names to avoid deployment conflicts.

---

## 4. Local Environment Setup

Create a Python virtual environment and install the deployment tooling.

### 4.1 Create and activate a virtual environment

```powershell
python -m venv .venv
.\.venv\Scripts\Activate.ps1
```

### 4.2 Install AWS Syndicate

```powershell
pip install aws-syndicate
```

### 4.3 Point Syndicate to the project config

```powershell
$env:SDCT_CONF = (Resolve-Path ".\.syndicate-config-restaurant-backend-app").Path
```

---

## 5. Build and Deploy

Run the following commands from the backend project directory.

### 5.1 Build the .NET solution

```powershell
dotnet build .\restaurant-backend-app.sln
```

### 5.2 Build the Syndicate bundle

```powershell
syndicate build
```

### 5.3 Deploy the application

```powershell
syndicate deploy
```
