# Security Configuration Guide

This document provides guidance on securely configuring the Meal Preparation Service API.

**Validates: Requirements 22.6**

## Environment Variables for Sensitive Data

For production deployments, it is **strongly recommended** to store sensitive configuration values in environment variables or secure configuration providers (Azure Key Vault, AWS Secrets Manager, etc.) instead of appsettings.json.

### Required Environment Variables

Set the following environment variables in your production environment:

```bash
# Database Connection
ConnectionStrings__DefaultConnection="Server=your-server;Database=MealPreparationService;User Id=your-user;Password=your-password;TrustServerCertificate=True"

# JWT Settings
JwtSettings__SecretKey="YourProductionSecretKeyThatIsAtLeast32CharactersLongAndSecure!"

# Google OAuth
GoogleOAuth__ClientId="your-google-client-id"

# OpenAI API
OpenAI__ApiKey="your-openai-api-key"

# Google Maps API
GoogleMaps__ApiKey="your-google-maps-api-key"

# VNPay Payment Gateway
VNPay__MerchantId="your-vnpay-merchant-id"
VNPay__HashSecret="your-vnpay-hash-secret"
```

### Using Environment Variables in .NET

The application automatically reads environment variables using the ASP.NET Core configuration system. Environment variables override values in appsettings.json.

Example in Program.cs:
```csharp
var secretKey = builder.Configuration["JwtSettings:SecretKey"];
var openAiKey = builder.Configuration["OpenAI:ApiKey"];
```

### Docker Environment Variables

When deploying with Docker, use the `-e` flag or docker-compose.yml:

```yaml
version: '3.8'
services:
  api:
    image: meal-preparation-service
    environment:
      - ConnectionStrings__DefaultConnection=${DB_CONNECTION}
      - JwtSettings__SecretKey=${JWT_SECRET}
      - OpenAI__ApiKey=${OPENAI_KEY}
      - GoogleMaps__ApiKey=${GOOGLE_MAPS_KEY}
      - VNPay__MerchantId=${VNPAY_MERCHANT_ID}
      - VNPay__HashSecret=${VNPAY_SECRET}
```

### Azure App Service Configuration

In Azure App Service, add application settings through the portal:
1. Navigate to Configuration > Application settings
2. Add each setting with the appropriate name (use double underscores for nested values)
3. Mark sensitive values as "Slot setting" if needed

### AWS Elastic Beanstalk Configuration

Use environment properties in your Elastic Beanstalk configuration:
```json
{
  "aws:elasticbeanstalk:application:environment": {
    "JwtSettings__SecretKey": "your-secret-key",
    "OpenAI__ApiKey": "your-api-key"
  }
}
```

## Security Best Practices

### 1. Never Commit Secrets to Source Control

- Add appsettings.Production.json to .gitignore
- Use user secrets for local development: `dotnet user-secrets set "OpenAI:ApiKey" "your-key"`
- Rotate secrets regularly

### 2. Use Strong Secret Keys

- JWT SecretKey: Minimum 32 characters, use cryptographically random values
- Generate secure keys: `openssl rand -base64 32`

### 3. Enable HTTPS Only

The application enforces HTTPS redirection. Ensure your hosting environment has valid SSL certificates.

### 4. Restrict CORS Origins

Update the `CorsOrigins` configuration to include only trusted frontend domains:

```json
"CorsOrigins": [
  "https://your-production-domain.com",
  "https://www.your-production-domain.com"
]
```

### 5. Database Security

- Use separate database users with minimal required permissions
- Enable SQL Server encryption (TDE)
- Use connection string encryption in configuration

### 6. API Key Rotation

Implement a key rotation strategy:
- OpenAI API keys: Rotate every 90 days
- Google Maps API keys: Use API restrictions (HTTP referrers, IP addresses)
- VNPay credentials: Follow VNPay's security guidelines

## Input Validation and Sanitization

The application implements multiple layers of security:

### 1. Input Sanitization Middleware

Automatically detects and blocks:
- SQL injection attempts
- XSS (Cross-Site Scripting) attacks
- Malicious query parameters

### 2. DTO Validation

All DTOs use Data Annotations for validation:
- Required fields
- String length limits
- Range validation
- Regular expression patterns

### 3. Log Sanitization

Sensitive data is automatically sanitized in logs:
- Passwords and API keys
- Credit card numbers
- Email addresses (partially masked)
- Phone numbers (partially masked)

## Data Protection

### Account Deletion and Anonymization

When users request account deletion (Requirement 22.7):
- Personal information is anonymized
- Transaction records are retained for compliance
- User data is marked as deleted

### Sensitive Data in Logs

The LogSanitizerService automatically removes:
- Passwords and password hashes
- API keys and access tokens
- Credit card information
- Personal identifiable information (PII)

## Monitoring and Auditing

### Log Levels

Configure appropriate log levels for each environment:

**Development:**
```json
"Logging": {
  "LogLevel": {
    "Default": "Debug",
    "Microsoft.AspNetCore": "Information"
  }
}
```

**Production:**
```json
"Logging": {
  "LogLevel": {
    "Default": "Warning",
    "Microsoft.AspNetCore": "Warning",
    "Microsoft.EntityFrameworkCore": "Error"
  }
}
```

### Security Events to Monitor

- Failed authentication attempts
- SQL injection/XSS detection
- Unauthorized access attempts
- Account deletion requests
- Payment transaction failures

## Compliance

### Data Retention

- User data: Anonymized upon deletion request
- Transaction records: Retained for 7 years (configurable)
- Logs: Retained for 30 days (configurable)

### GDPR Compliance

The application supports GDPR requirements:
- Right to be forgotten (account deletion with anonymization)
- Data export (implement as needed)
- Consent management (implement as needed)

## Emergency Procedures

### In Case of Security Breach

1. Immediately rotate all API keys and secrets
2. Review logs for suspicious activity
3. Notify affected users if personal data was compromised
4. Update security measures to prevent future incidents

### Key Rotation Procedure

1. Generate new keys/secrets
2. Update environment variables in production
3. Restart application services
4. Verify functionality
5. Revoke old keys after grace period

## Contact

For security concerns or to report vulnerabilities, contact: security@mealprep.example.com
