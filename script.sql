-- Note: Replace MANAGED-IDENTITY-NAME with the actual managed identity name from deployment
-- The deploy.sh script will need to update this file with the actual name

-- Drop and recreate the managed identity user with correct SID
IF EXISTS (SELECT * FROM sys.database_principals WHERE name = 'MANAGED-IDENTITY-NAME')
BEGIN
    DROP USER [MANAGED-IDENTITY-NAME];
END

CREATE USER [MANAGED-IDENTITY-NAME] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [MANAGED-IDENTITY-NAME];
ALTER ROLE db_datawriter ADD MEMBER [MANAGED-IDENTITY-NAME];
