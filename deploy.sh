dotnet publish -o ./publish
aws cloudformation package --template-file template.yaml --s3-bucket san-google-oauth-proxy --output-template-file template-output.yaml --region eu-west-1 --profile dev
aws cloudformation deploy --template-file template-output.yaml --stack-name san-google-oauth-proxy --capabilities CAPABILITY_IAM --region eu-west-1 --profile dev
