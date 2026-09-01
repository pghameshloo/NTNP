# Administrator Guide

For server installation, backup, and the MSI build, see `deployment.md`. This guide covers ongoing
administration from inside the application itself — everything an Admin user does from the desktop
client's **مدیریت سیستم** (System Management) menu group and the master-data screens under
**اطلاعات پایه** (Basic Information).

## Roles (Section 6)

| Role | Persian label | Can do |
|---|---|---|
| Admin | مدیر سیستم | Everything below, plus Users/Roles, Audit Log, Company Settings, Currencies, Pricing Profiles, Equipment. |
| Engineering | مهندسی | Manage Panel Templates and BODY+ES templates. |
| Commercial | بازرگانی | Manage Customers, create/edit Projects and project lineups. |
| Approver | تأییدکننده | Decide (approve/reject) and lock project revisions. |
| Viewer | بیننده | Read-only access to everything the other roles can see. |

Every permission is enforced **server-side** on every API call — the desktop client hides buttons a
user's role can't use as a convenience, but the server would refuse the action even if a hidden
button were somehow triggered (Section 6: "hiding a button is not authorization"). A user can hold
more than one role at once (e.g. Admin + Approver).

## User management

**System Management → کاربران (Users)**

- **New user**: click "+ کاربر جدید", enter email, display name, an initial password, and tick every
  role that applies. The email doubles as the username.
- **Edit**: select a user, change their display name, active/inactive flag, or role set, then Save.
  Deactivating a user (unchecking "فعال") blocks their login immediately without deleting their
  history — approvals, audit entries, and created records they authored keep their name attached.
- **Reset password**: select a user, type a new password into the "رمز عبور" field, click "بازنشانی
  رمز عبور". The user should change it again on next login if your policy requires that (this
  release does not force a password change on next login — track that separately if needed).
- **The very first Admin account** on a fresh production database is created outside the app, via
  the `create-admin` command-line utility (`deployment/database/create-admin.ps1`) — see
  `deployment.md` §3. After that, every further user (including further Admins) is created from
  this screen.

## Company and report settings

**System Management → تنظیمات (Settings)**

Two tabs over the same record:

- **اطلاعات شرکت (Company Information)**: legal name (Persian/English), address, phone, email,
  website, and the exchange-rate staleness threshold (days) used to flag equipment prices as expired
  on the Equipment Database screen and dashboard.
- **قالب گزارش و پیشنهاد قیمت (Report Template)**: the customer quotation's default title
  (Persian/English), the confidentiality label printed on internal reports, default commercial terms
  (delivery, payment, warranty, inspection, packing, transportation, taxes/duties, scope exclusions),
  and the signature block (prepared-by, commercial manager, managing director names/positions) that
  appears on every generated quotation PDF. Changes here apply to every quotation generated
  afterward — they do not retroactively change already-issued PDFs (which are immutable snapshots,
  same principle as an approved project revision).

Logo/stamp images are configured via `LogoStoragePath`/`StampImageStoragePath` on this same record —
upload support for these through the UI is a natural next-release addition; for this release, set
the storage path directly via the settings API/database to point at a file already placed under the
server's `FileStorage:RootPath` (see `deployment.md` — `ASSUMPTIONS.md` §10 covers the current
placeholder logo situation).

## Currencies and exchange rates

**System Management → اطلاعات پایه → نرخ ارز (Currencies)**

- **Add a currency**: "+ ارز جدید", enter the ISO 4217 code, name, and symbol. The very first
  currency added to a fresh database should be IRR (marked as the base currency automatically).
- **Add a rate**: select a currency, fill in the purchase and selling rates (both to IRR), the
  effective date, and an optional source/notes, then "ثبت نرخ". Rates are versioned — adding a new
  one never overwrites history, and every equipment price and project line permanently snapshots the
  rate that was active when it was calculated (Section 9/"price and exchange-rate snapshots are
  preserved").
- A rate older than the Company Settings staleness threshold shows a "منقضی" (Expired) badge.

## Equipment Database

**اطلاعات پایه → بانک تجهیزات (Equipment Database)** — Admin manages this centrally; Engineering
and Commercial have read access for reference.

- **Manual entry**: "+ تجهیز جدید", fill in code (must be unique), Persian/English descriptions,
  category/brand/model/manufacturer/supplier, unit, and lead time, then Save. Add its first price
  from the "تاریخچه قیمت" (Price History) tab immediately after — a piece of equipment with no price
  cannot be used in a panel template's BOM.
- **Excel import**: "درون‌ریزی از اکسل" — pick a `.xlsx` file; the tool previews insert/update/error
  counts (Section 9's 10-step workflow: select → preview → validate → confirm → commit, all inside
  this one dialog flow) before anything is written, and shows every row-level error before you
  confirm. See `docs/excel-mapping.md` for the expected column layout.
- **Missing/expired price filter**: the "فقط بدون قیمت" checkbox on the search bar surfaces
  equipment that would block a project's BOM generation.

## Panel Templates, BODY+ES, Pricing Profiles

Engineering owns Panel Templates and BODY+ES templates (their BOM editors are inline on the
template's detail pane — add a component, adjust quantity/waste%/cost multiplier, save). Admin owns
Pricing Profiles (default markup/gross-margin rate, Rial/Foreign share, rounding policy,
reconciliation tolerance) that Commercial can apply as a starting point when creating a new project,
then adjust per-project as needed. A template must be **تأیید (Approved)** before it can be selected
when adding a line to a project — this is enforced server-side, not just hidden in the picker.

## Audit Log

**System Management → گزارش فعالیت‌ها (Audit Log)** — Admin only. Every create/update/delete,
price change, exchange-rate change, BOM/BODY+ES change, template approval, project line change
(including every manual override and its required reason), revision creation, approval decision,
lock, import, and report issuance is recorded with who, when, old/new values (as JSON), and — for
overrides — the mandatory reason text. Filter by entity type, entity ID, date range. This log is
append-only; nothing in the application ever deletes an audit entry.

## Server Connection Settings (per desktop installation)

**System Management → اتصال به سرور (Server Connection Settings)** — lets any user on this machine
point their client at a different server address (e.g. a pilot environment) without reinstalling.
The Windows installer sets an initial machine-wide default during setup (stored in
`HKLM\Software\NTNP\Pricing\ServerUrl`); this screen's "ذخیره" (Save) only changes *this Windows
user's* override, stored in `%LOCALAPPDATA%\NTNP\Pricing\client-settings.json` — it never requires
admin rights and never affects other users on a shared machine. "آزمایش اتصال" (Test Connection)
reports whether the server is reachable and whether its database is reachable, separately, so a
"server up, database down" condition is distinguishable from "server unreachable".
