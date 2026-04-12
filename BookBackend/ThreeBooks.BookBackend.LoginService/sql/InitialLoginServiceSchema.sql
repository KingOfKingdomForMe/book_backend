CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `permissions` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `Code` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    `Name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    `CreatedAtUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_permissions` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `roles` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `Code` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    `Name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    `CreatedAtUtc` datetime(6) NOT NULL,
    `UpdatedAtUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_roles` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `users` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `Username` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    `DisplayName` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    `Mobile` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
    `IsActive` tinyint(1) NOT NULL,
    `FailedPasswordAttemptCount` int NOT NULL,
    `LockoutEndUtc` datetime(6) NULL,
    `CreatedAtUtc` datetime(6) NOT NULL,
    `UpdatedAtUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_users` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `role_permissions` (
    `RoleId` char(36) COLLATE ascii_general_ci NOT NULL,
    `PermissionId` char(36) COLLATE ascii_general_ci NOT NULL,
    `CreatedAtUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_role_permissions` PRIMARY KEY (`RoleId`, `PermissionId`),
    CONSTRAINT `FK_role_permissions_permissions_PermissionId` FOREIGN KEY (`PermissionId`) REFERENCES `permissions` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_role_permissions_roles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `roles` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `audit_logs` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `UserId` char(36) COLLATE ascii_general_ci NULL,
    `Action` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    `Detail` varchar(512) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    `IpAddress` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
    `UserAgent` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
    `CreatedAtUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_audit_logs` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_audit_logs_users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE SET NULL
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `login_attempts` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `UserId` char(36) COLLATE ascii_general_ci NULL,
    `Identity` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    `Succeeded` tinyint(1) NOT NULL,
    `FailureReason` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
    `IpAddress` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
    `UserAgent` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
    `CreatedAtUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_login_attempts` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_login_attempts_users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE SET NULL
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `refresh_tokens` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
    `TokenHash` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    `ExpiresAtUtc` datetime(6) NOT NULL,
    `CreatedAtUtc` datetime(6) NOT NULL,
    `CreatedByIp` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
    `CreatedByUserAgent` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
    `RevokedAtUtc` datetime(6) NULL,
    `RevokedReason` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
    `ReplacedByTokenId` char(36) COLLATE ascii_general_ci NULL,
    CONSTRAINT `PK_refresh_tokens` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_refresh_tokens_users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `user_credentials` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
    `Provider` int NOT NULL,
    `Subject` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    `SecretHash` varchar(512) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    `IsPrimary` tinyint(1) NOT NULL,
    `CreatedAtUtc` datetime(6) NOT NULL,
    `UpdatedAtUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_user_credentials` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_user_credentials_users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `user_roles` (
    `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
    `RoleId` char(36) COLLATE ascii_general_ci NOT NULL,
    `CreatedAtUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_user_roles` PRIMARY KEY (`UserId`, `RoleId`),
    CONSTRAINT `FK_user_roles_roles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `roles` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_user_roles_users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE INDEX `IX_audit_logs_UserId_CreatedAtUtc` ON `audit_logs` (`UserId`, `CreatedAtUtc`);

CREATE INDEX `IX_login_attempts_Identity_CreatedAtUtc` ON `login_attempts` (`Identity`, `CreatedAtUtc`);

CREATE INDEX `IX_login_attempts_UserId` ON `login_attempts` (`UserId`);

CREATE UNIQUE INDEX `IX_permissions_Code` ON `permissions` (`Code`);

CREATE UNIQUE INDEX `IX_refresh_tokens_TokenHash` ON `refresh_tokens` (`TokenHash`);

CREATE INDEX `IX_refresh_tokens_UserId` ON `refresh_tokens` (`UserId`);

CREATE INDEX `IX_role_permissions_PermissionId` ON `role_permissions` (`PermissionId`);

CREATE UNIQUE INDEX `IX_roles_Code` ON `roles` (`Code`);

CREATE UNIQUE INDEX `IX_user_credentials_Provider_Subject` ON `user_credentials` (`Provider`, `Subject`);

CREATE INDEX `IX_user_credentials_UserId` ON `user_credentials` (`UserId`);

CREATE INDEX `IX_user_roles_RoleId` ON `user_roles` (`RoleId`);

CREATE UNIQUE INDEX `IX_users_Mobile` ON `users` (`Mobile`);

CREATE UNIQUE INDEX `IX_users_Username` ON `users` (`Username`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260412094225_InitialLoginServiceSchema', '8.0.10');

COMMIT;

