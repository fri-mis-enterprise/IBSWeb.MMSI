# MSAP integration

IBSWeb.MMSI is the host. MSAP's Job Order, Dispatch Ticket, tariff, billing, collection, vessel scheduling, master files, imports, reports, permissions and audit workflows live in dedicated module paths.

| Boundary | Location |
| --- | --- |
| Operational controllers and views | `IBSWeb/Areas/MSAP` |
| MSAP administration | `IBSWeb/Areas/MSAPAdmin` |
| MSAP document maintenance | `IBSWeb/Areas/MSAPSuperAdmin` |
| Models and viewmodels | `IBS.Models/MSAP` |
| DTOs, repositories, services and utilities | Each existing project's `MSAP` folder |
| Startup, scoped JSON and dependencies | `IBSWeb/MSAP` and `IBS.Utility/MSAP/MSAP.props` |
| Static assets | `IBSWeb/wwwroot/msap` |
| Database and migration history | PostgreSQL schema `msap` |
| Audit trail | `msap.audit_trails`, `IBS.Models.MSAP.AuditTrail`, `/MSAP/AuditTrail` |
| Development attachments | `IBSWeb/App_Data/MSAP/LocalStorage`, `/msap-storage` |
| Cloud attachments | `msap/` object prefix, separately configured MSAP storage options |

MSAP reuses the host's Identity users, roles, login and cookie. The module context reads the existing `public."AspNetUsers"`, `public."AspNetRoles"` and `public."AspNetUserRoles"` tables; its migrations exclude them. Account and role administration operates on this shared identity directory. MSAP permissions, company/reference data, posted periods and audits remain separate. The host's company selection is not required to open the MMSI maritime dashboard.

The base context, models, audit trail, historical migrations and existing routes were not changed. The base has four integration files with additive edits: `IBSWeb/Program.cs`, `IBSWeb/Views/Shared/_Navbar.cshtml`, `IBSWeb/IBSWeb.csproj` and `IBS.Utility/IBS.Utility.csproj`. New dependencies and manual publication rules are in module-owned `.props` files. The local development connection now targets `ibs_dev`.

## Database setup

Local setup completed on 2026-10-06 against `localhost:5432/ibs_dev`: MSAP migration `20261006081123_InitialMsapModule` applied, with one company and 21 payment terms seeded. Public table counts and schema, Identity contents and base audit contents were verified unchanged. MSAP starts with empty operational records; existing accounting data remains in `public`. The integrated host's `IBSWeb/appsettings.Development.json` uses this database. Normal startup does not require `MSAP__ApplyMigrations=true` after this initialization.

The host database and Identity tables must already be initialized using the base's normal deployment process. MSAP uses the same `ConnectionStrings:MMSIConnection`. Its fresh migration creates only module-owned tables; it does not replay the separate source repository's historical migrations or copy existing source data.

For an explicit schema deployment, from the repository root, supply the same connection through `ConnectionStrings__MMSIConnection` and run:

```powershell
dotnet ef database update --project IBS.DataAccess --startup-project IBSWeb --context IBS.DataAccess.MSAP.Data.MsapDbContext
```

For startup migration and reference-data initialization, enable `MSAP:ApplyMigrations` (environment variable `MSAP__ApplyMigrations=true`). It defaults to false. Initialization seeds MSAP's company and payment terms, never users, passwords or roles. Running it again preserves existing module records.

The EF design factory reads `ConnectionStrings__MMSIConnection`; its placeholder connection is for generating migrations only. Use the same explicit environment variable when creating future MSAP migrations:

```powershell
dotnet ef migrations add ChangeName --project IBS.DataAccess --startup-project IBSWeb --context IBS.DataAccess.MSAP.Data.MsapDbContext --output-dir MSAP/Migrations
```

Keep base migrations under their existing context and path. Do not use the MSAP context to change base tables.

### Table layout and imports

`ibs_dev` contains the existing host tables in `public` and 28 MSAP tables (including migration history) in `msap`:

| Purpose | Tables in `msap` |
| --- | --- |
| Workflow | `msap_job_orders`, `msap_dispatch_tickets`, `msap_billings`, `msap_collections`, `msap_collection_bills` |
| Maritime references | `msap_ports`, `msap_terminals`, `msap_vessels`, `msap_services`, `msap_principals`, `msap_tariff_rates`, `msap_tugboats`, `msap_tug_masters`, `msap_tugboat_owners` |
| Master files | `customers`, `suppliers`, `employees`, `bank_accounts`, `companies`, `terms` |
| Scheduling and access | `msap_vessel_schedules`, `msap_user_accesses`, `msap_posted_periods` |
| Audit and settings | `audit_trails`, `app_settings`, `notification`, `user_notification`, `__EFMigrationsHistory` |

Use fully qualified names in SQL, for example `msap.msap_billings` and `msap.customers`. EF queries already apply this schema. Import/reset statements and legacy ID sequence updates also explicitly use `msap`; PostgreSQL's default search path does not include it. A missing-table error from an unqualified import statement is a code issue, not a reason to replay migrations.

Billing imports retain the source behavior of clearing existing MSAP billings and dispatch tickets before loading their CSVs. Reset clears the module's imported maritime data. Both operations are transactional; reset records an MSAP audit entry.

## Storage and audit identifiers

Development storage can be configured through `MSAP:LocalStoragePath`. Production storage uses `MSAP:GoogleCloudStorageBucketName` and the existing cloud credential mechanism. The `msap/` object prefix separates attachments even when a bucket is shared. Browser quick-access state and theme keys are also separate.

An audit record's identity is its module, document type and record ID. `RecordId` may overlap between documents or with the base application. MSAP timelines query only MSAP records and audits. Job Order, Dispatch Ticket and Billing creation save the generated ID before recording the audit, within a transaction. BAF adjustments reference the dispatch ticket; collection edits retain their collection ID and reference.

## Upstream synchronization

Sync upstream normally and retain the four small integration edits. Keep future MSAP changes in the module paths above. Shared integration lines can still require a merge resolution; folder isolation cannot promise zero conflicts. Build and run the checks after each sync:

```powershell
dotnet build "Integrated Business System.sln"
dotnet run --project Checks/MSAP/Check.csproj
```

The check verifies all imported controllers, routing, DI, view isolation, database ownership, audit ownership, scoped JSON and attachment path confinement without starting a server.

For the PostgreSQL integration check, set `MSAP_CHECK_SERVER_CONNECTION` to a local PostgreSQL server connection with permission to create a temporary database, then run:

```powershell
dotnet run --project Checks/MSAP/Check.csproj -- --database
```

When the sibling MSAP copy is present, the check can read its development connection instead. It creates a uniquely named temporary database and drops only that database afterward. It verifies unchanged base schema/audits, shared users, idempotent module migration/seeding, Job Order/dispatch/billing/posting/collection, VAT totals and audit timeline isolation.

Browser layout, guided tours, file previews, manual links and report downloads still require manual review. Existing source data and attachments have not been migrated.
