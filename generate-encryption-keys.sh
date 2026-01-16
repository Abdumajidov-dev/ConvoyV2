#!/bin/bash
# AES-256 encryption keys generator for Linux/Mac
# Run: bash generate-encryption-keys.sh

echo "🔐 Generating AES-256 Encryption Keys..."
echo ""

# Generate 32 byte (256-bit) key
KEY=$(openssl rand -base64 32)

# Generate 16 byte (128-bit) IV
IV=$(openssl rand -base64 16)

echo "✅ Keys generated successfully!"
echo ""
echo "Add these to your appsettings.json:"
echo ""
echo '  "Encryption": {'
echo '    "Enabled": true,'
echo "    \"Key\": \"$KEY\","
echo "    \"IV\": \"$IV\""
echo '  }'
echo ""
echo "⚠️  IMPORTANT SECURITY NOTES:"
echo "   1. Keep these keys SECRET - never commit to Git!"
echo "   2. Use different keys for Development and Production"
echo "   3. Store production keys in secure vault (Azure Key Vault, AWS Secrets Manager)"
echo "   4. Share keys with Flutter team via secure channel (NOT email/Slack)"
echo ""
echo "🔄 For Flutter (Dart), use these same Base64 strings"
echo ""

# Save to file
OUTPUT_FILE="encryption-keys-$(date +%Y-%m-%d-%H%M%S).txt"
cat > "$OUTPUT_FILE" <<EOF
Generated: $(date)

appsettings.json:
  "Encryption": {
    "Enabled": true,
    "Key": "$KEY",
    "IV": "$IV"
  }

Flutter (Dart):
  final String encryptionKey = '$KEY';
  final String encryptionIV = '$IV';
EOF

echo "💾 Keys saved to: $OUTPUT_FILE"
echo ""
