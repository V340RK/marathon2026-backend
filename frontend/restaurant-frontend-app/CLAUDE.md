# restaurant-frontend-app — Angular 22 SPA

Booking tables, reservations and pre-orders for three roles: **client**, **waiter**, **admin**.
Standalone components, SCSS, Karma + Jasmine. Deploy target: S3 static hosting.

This file is the single source of truth for frontend work. The backend has no `CLAUDE.md` yet — documenting it is a separate task.

**The backend cannot be run locally.** No Docker, no LocalStack, no `dotnet run` host. Frontend work runs against an already-deployed API Gateway URL; any backend change requires a real `syndicate deploy` by whoever owns that task. When a frontend model disagrees with an API response, `backend/restaurant-backend-app/docs/ApiRoutes.md` and `dnapp/lambdas/api-handler/Routing/RouteRegistry.cs` win over the frontend interface. Region note: the backend config deploys to `us-west-1` while the frontend dev environment points at an API in `eu-west-1`.

---

## 1. Quick start

```bash
npm ci                 # NOT npm install — the lockfile is authoritative
npm start              # dev server on http://localhost:4200
```

`ng serve` uses the `development` configuration → swaps `environment.ts` for `environment.development.ts` (dev API URL baked in). No env vars needed, no AWS needed.

Production build:

```bash
BACKEND_URL=https://<api-id>.execute-api.<region>.amazonaws.com/develop npm run build
```

`BACKEND_URL` is **mandatory** — see [§3](#3-environment-configuration).

---

## 2. Commands

| Command | Notes |
|---|---|
| `npm start` | dev server, `development` config |
| `npm run build` | **production** config (it is the `defaultConfiguration`); requires `BACKEND_URL` |
| `npm test` | Karma + Jasmine, needs Chrome |
| `npm run lint` | 216 pre-existing violations — see [§8](#8-known-debt-measured--re-check-do-not-re-measure) |
| `npm run lint-fix` | ~40 of them auto-fix |
| `npm run format` | Prettier over `src/**/*.{ts,html,scss}` |

---

## 3. Environment configuration

| File | Used by | In git |
|---|---|---|
| `src/environments/environment.ts` | never at runtime — holds `BACKEND_URL_PLACEHOLDER` | yes |
| `src/environments/environment.development.ts` | `ng serve` / `--configuration development` | yes |
| `src/environments/environment.production.ts` | `--configuration production` | **no — generated** |

npm hooks in `package.json`:

- `prebuild` → `scripts/set-env.mjs` writes `environment.production.ts` from `$BACKEND_URL`. Missing or malformed → the build aborts with a usage message, so a placeholder bundle cannot be produced.
- `postbuild` → `scripts/check-bundle.mjs` scans `dist/**/*.{js,css,html}`; any `BACKEND_URL_PLACEHOLDER` left → non-zero exit. A successful run prints `Bundle check passed`.

Never commit `environment.production.ts`; never hardcode an API URL in it. To guard a new placeholder, extend `FORBIDDEN` in `scripts/check-bundle.mjs`.

---

## 4. Code structure — where things live

```
src/app/
  auth/                     self-contained auth module
    components/             login, register, profile, password inputs
    guards/                 admin.guard.ts
    models/                 user.model.ts, login.model.ts, tokens.model.ts
    services/               authentication.service.ts, form-validators-service.service.ts
    token.interceptor.ts
  cart/                     cart feature (component + child modals)
  components/               feature components grouped by domain
    dish-modal/ location-overview/ place-order-waiter/ reservations/ tables/
  core/                     app-wide building blocks
    components/             feedback-window, waiter-dashboard, reporting-dashboard
    mocks/
  pages/                    route-level components (one folder per page)
    menu-page/
  main-page/                landing page (legacy location — new pages go to pages/)
  models/                   shared domain interfaces, one folder per domain
    cart/ dishes/ enums/ feedbacks/ locations/ notifications/ orders/
    profile-img/ reports/ reservations/ shared/ tables/
  services/                 HTTP services, one folder per domain
    dishes/ feedbacks/ locations/ notifications/ pre-orders/ reports/
    reservation/ table/ toast/ users/ waiters/
  shared/                   reusable, feature-agnostic code
    constants/              roles
    custom-components/      button, select, password, header, dish-card, ...
    helpers/                date-helpers.ts
    layouts/  mappers/  models/  styles/
```

**Decision table — where does my new file go?**

| What you are adding | Location | Naming |
|---|---|---|
| Route-level screen | `pages/<name>-page/` | `menu-page.component.ts` |
| Feature component used by one domain | `components/<domain>/` | `dish-modal.component.ts` |
| Reusable dumb component | `shared/custom-components/<name>/` | `custom-button.component.ts` |
| HTTP call to the API | `services/<domain>/` | `feedbacks.service.ts` |
| Shared domain interface | `models/<domain>/` | `cart.model.ts` |
| Interface used by one component only | same folder as the component, in `models.ts` | — |
| Constants | `shared/constants/` (global) or a local `constants.ts` | — |
| Route guard | `auth/guards/` or `<feature>/guards/` | `admin.guard.ts` |
| Pure function / date math | `shared/helpers/` | `date-helpers.ts` |

Rules: **one interface file per concern** — never declare an interface inside a component file. **No variable names shorter than 3 characters.** There are currently 0 barrel `index.ts` files; do not add them piecemeal, that is a separate refactor (board: `[FE] Root project structure refactoring`).

---

## 5. Conventions

### Enforced now — follow immediately

- **Standalone components only.** No `NgModules`. Declare deps in `imports: []` and remove entries the template does not use (Angular warns `NG8113`).
- **`inject()` over constructor injection.** `@angular-eslint/prefer-inject` is an *error*. Remember to import `inject` from `@angular/core` — forgetting it has broken the build.
- **TypeScript strict mode** (`strict`, `noImplicitOverride`, `noImplicitReturns`, `noPropertyAccessFromIndexSignature`, `noFallthroughCasesInSwitch`). No `any`; the 11 existing violations are debt, do not add more.
- **SCSS:** `@use`, never `@import`. Namespaced colour functions — `color.adjust($c, $lightness: -10%)`, not the deprecated `darken()`.
- **File naming:** `*.component.ts`, `*.service.ts`, `*.model.ts`, `*.guard.ts`, `*.resolver.ts`, `*.store.ts`, `*.spec.ts`.
- **Relative imports** are the current norm (`../../services/...`). Count the `../` levels — wrong depth is the single most common way this repo has been broken.
- **`ChangeDetectionStrategy.Eager`** is what all 45 components use today. Do not flip individual components to `OnPush` in unrelated PRs; it is a coordinated migration.

### Target state — agreed direction, not yet reality

Check before relying on any of these, and introduce them only inside the task that owns the migration.

| Target | Reality today | Tracking issue |
|---|---|---|
| `input()` / `output()` signal APIs | 87 `@Input()`, 51 `@Output()`, signals in 2 files | #6 Migration to signals approach |
| NgRx SignalStore (`@ngrx/signals`), `toSignal()` at component boundary | package not installed, no `store/` folder | #6 |
| Fully zoneless | `zone.js` still in `angular.json` polyfills (build **and** test) | #31 Ensure application is fully zoneless |
| `OnPush` everywhere | 45× `ChangeDetectionStrategy.Eager` | #31 |
| `@Service` instead of `@Injectable` | 16× `@Injectable` | #10 |
| `app/...` absolute imports | no `baseUrl`/`paths` in any tsconfig — `app/...` will not resolve | #24, #51 (duplicates, merge them) |
| `httpResource()` for GETs | every service uses `HttpClient` | — |
| Barrel `index.ts` per folder | 0 barrels | #5 |

Claims that circulate in older notes and are **false for this repo**: there is no SSR/hydration setup (no `@angular/ssr`, no `server.ts`), and `npm run build` does **not** set `--base-href /learn-and-code/`. Ignore any rule that depends on them.

---

## 6. Testing — required, not optional

Baseline today: 56 `.spec.ts` files, 57 `it()` blocks, of which **11 are untouched CLI stubs** (`should be created`), and only **1** spec configures providers. This is a floor to raise, not a norm to copy.

### What must be covered

| Code | Minimum test |
|---|---|
| Service method with HTTP | request URL + method + body, success mapping, error path |
| Guard / interceptor | allow case, deny case, redirect target |
| Pure helper (`date-helpers.ts`, mappers) | table-driven cases incl. boundaries: midnight, DST, empty input |
| Component with logic | inputs → rendered output, user event → emitted output, empty/loading/error states |
| Bug fix | a test that fails before the fix and passes after |

Purely presentational components need nothing beyond a creation test.

### Patterns

```ts
TestBed.configureTestingModule({
  providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
});
```

- Anything touching `HttpClient` needs `provideHttpClient()` + `provideHttpClientTesting()`; anything injecting `Router` needs `provideRouter([])`. Missing providers is why stub specs fail at runtime even though they compile.
- Assert HTTP through `HttpTestingController`: `expectOne(url)`, check `req.request.method` / `req.request.body`, `flush(payload)`, then `httpMock.verify()` in `afterEach`.
- One behaviour per `it()`, arrange-act-assert, descriptive names — `'returns Unknown for anonymous feedback'`, not `'works'`.
- Never assert on `console` output. Never leave `fit` / `fdescribe` / `xit` in a commit.
- Replacing a stub spec beats deleting it — turn `should be created` into a real assertion.

### Verification gates — all four before opening a PR

```bash
npx tsc -p tsconfig.app.json  --noEmit          # app sources
npx tsc -p tsconfig.spec.json --noEmit          # specs break independently of the app
BACKEND_URL=https://example.com npm run build   # must end with "Bundle check passed"
npm test -- --watch=false
```

`tsconfig.app.json` has `files: ["src/main.ts"]`, so **files unreachable from the app graph are never type-checked** — dead code with broken imports passes `ng build` silently. After adding or moving files outside that graph, verify every relative import resolves.

---

## 7. Best practices specific to this codebase

- **Kill subscriptions.** Anything subscribing or polling (`header.component.ts` uses `interval`) must unsubscribe in `ngOnDestroy` or use `takeUntilDestroyed()`. Leaks here survive route changes.
- **Type every HTTP call** — `this.http.get<LocationResponse[]>(url)`. Never `get<any>`.
- **Errors reach the user**, not just the console — surface them via `services/toast/toast.service.ts`.
- **No business logic in templates.** Precompute in the component; template expressions run on every change detection cycle.
- **Keyboard parity for every `(click)`** — add `(keydown.enter)` and `tabindex`, or use a `<button>`. This is 98 of the 216 lint errors; do not add the 99th.
- **Watch the bundle.** 915.96 kB against a **1 MB hard error budget** — about 84 kB of headroom. Import heavy libraries lazily or not at all; read the size line printed by every build.
- **No new npm packages without asking.** Three date libraries (`moment`, `dayjs`, `date-fns`) are already installed — reuse, do not add a fourth.
- **Every data-driven screen needs empty / loading / error states.** A freshly deployed backend has empty DynamoDB tables, so "no data" is a normal case, not an edge case.
- **When a model disagrees with the API**, check `backend/restaurant-backend-app/docs/ApiRoutes.md` and `RouteRegistry.cs` — the backend is the source of truth.

---

## 8. Known debt (measured — re-check, do not re-measure)

**Build economics**

- Initial bundle **915.96 kB** raw / 210 kB transfer. Budgets: warning 500 kB (exceeded), **error 1 MB (~84 kB headroom)**.
- 5 component stylesheets exceed the 4 kB style budget (`header.component.scss` at 8.15 kB is worst; error threshold 10 kB).
- The production build **inlines Google Fonts over the network**. Fine on a laptop, `403` and a failed build in a restricted CI. Fix when needed: `"optimization": { "fonts": { "inline": false } }` or self-host Poppins.
- `dist` ≈ **69 MB**, ~60 MB of it `assets/img`, individual JPEGs up to 12 MB. Unoptimised.
- CommonJS bailouts: `dayjs`, `dayjs/plugin/*` (via `ngx-daterangepicker-bootstrap`), `qr-code-styling`.
- `npm audit`: 17 vulnerabilities (12 high). Do **not** run `npm audit fix` blindly on Angular 22.

**Code quality**

- `npm run lint`: 216 errors / 1 warning — a11y `click-events-have-key-events` 50, `interactive-supports-focus` 48, `prefer-inject` 43, `no-inferrable-types` 22, `no-unused-vars` 14, `no-explicit-any` 11, `prefer-const` 9, `no-var` 7, template `eqeqeq` 6. Worst files: `header.component.html` (26), `waiter-make-reservation.component.html` (11), `date-helpers.ts` (10).
- Prettier: 66 files unformatted.
- 11 spec files are still CLI stubs; only 1 spec sets up providers.

**Missing infrastructure**

No CI, no branch protection, no `.nvmrc`/`engines`, no `buildspec.yml`/`amplify.yml`. Until the pipeline exists (board: `[DO] Implement Github Actions CI/CD pipeline`), the four gates in §6 are the only thing standing between a broken build and `main`. It has already reached `main` twice.

---

## 9. Traps that have already broken this repo

1. **Import depth.** `../` vs `../../` broke `main` for everyone — compile before pushing.
2. **`npm install` vs `npm ci`.** A desynced lockfile let `npm install` pass while `npm ci` failed; CI would have broken and nobody would have noticed locally.
3. **Node version.** Angular 22 needs Node ≥ 20.19. Nothing in the repo enforces it yet; local dev runs Node 22.
4. **Dead code hides breakage** — see the `tsconfig.app.json` note in §6.
5. **macOS case-insensitivity.** `import './UserService'` for `user.service.ts` builds locally and fails on Linux CI. Verify on Linux before pushing:
   `docker run --rm -v "$PWD":/app -w /app public.ecr.aws/docker/library/node:22-alpine sh -lc "npm ci && BACKEND_URL=https://example.com npm run build"`
6. **macOS duplicate files.** Files named `foo 2.ts` (with a space) have reached `main` twice. They import nothing real and nothing imports them — delete on sight.
7. **Never text-replace a type name.** Use IDE symbol rename. A global `User` → `UserModel` replace once rewrote log messages, an `alt` attribute, a visible UI label and the comparison `fullName === 'Anonymous User'`, silently breaking anonymous feedback.

---

## 10. Working agreements

- **Issues** follow the board title format: `[FE]` frontend, `[DO]` devops/infra; backend issues currently carry no prefix. Columns: Backlog → Ready → In progress → In review → Done. WIP limit on In progress is 3.
- **Branches:** `feature/<short-description>`, `bugfix/<short-description>`, `fix/<short-description>`. Never commit to `main` directly. Rebase strategy — no merge commits inside a feature branch.
- **Commit message:** `<package.json version> short description of the change`, ≤ 120 characters; multiple changes separated by commas. Bump `version` when the change is user-visible.
- **One logical change per PR.** Never mix a lint sweep or a formatting pass with a behaviour change.
- **Every PR that changes behaviour ships tests.** No test → say in the PR description why it is untestable.
- **Do not commit:** `.idea/`, `.vscode/`, `node_modules/`, `dist/`, `environment.production.ts`, unrelated `package-lock.json` churn. When the lockfile does change, say why in the PR description.

---

## 11. For AI agents working in this app

- Do **not** create commits, branches, tags, or push unless explicitly asked.
- Run the four gates in §6 before claiming something works. Report exact numbers (bundle size, error counts, test counts), never impressions.
- Separate **current** from **target** (§5) before applying a rule. Roughly half of the agreed conventions are not in the code yet, and applying them piecemeal creates inconsistency worse than the debt.
- History note: PR #43 proposed an earlier version of this file that stated several things which are false here — SSR/hydration rules, a `--base-href /learn-and-code/` build flag, "fully zoneless", `app/...` imports. It was closed. Do not reintroduce those claims without checking the code first.
- Do not add a second `CLAUDE.md` for this app, and do not copy these rules into the repo root — one file, one owner.
