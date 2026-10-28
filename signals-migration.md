# Signals-first conventions

This project is migrating to Angular Signals. New UI code should follow signals-first patterns.

## Component state 
- Use `signal()` for local mutable UI state (e.g. toggles, UI flags, selected item).
- Use `computed()` for derived values (e.g. filtered lists, counters).
- Use `effect()` for side effects triggered by signal changes (e.g. start/stop polling, sync with services).

> Note: In Angular v22, `effect()` must be created in an injection context (e.g. constructor / field initializer / `runInInjectionContext`).

## Inputs / Outputs (standalone components)
- Prefer `input()` / `output()` (InputSignal / OutputEmmiterRef) when creating new standalone components.
- Avoid introducing new `@Input()` / `@Output()` where signals-based API is feasible.

## RxJS / HTTP interop
- HTTP and data streams can remain RxJS-based.
- In components, prefer converting streams to signals:
    - `toSignal(observable$, { initialValue })` when you want to render stream data in templates.
    - Use `effect()` when you need side effects on updates (logging, start/stop, etc.).

## Rule: no new Subject/BehaviorSubject for UI state
Do not create new `Subject` / `BehaviorSubject` instances for component UI state.
Use signals instead. RxJS is still fine for HTTP and event streams where it makes sense.

## Testing template (standalone + signals)
When component template uses router directives (`routerLink`, `routerLinkActive`) add router providers:
- `provideRouter([])` in TestBed providers.

When component depends on services/observables:
- mock services using `jasmine.createSpyObj`
- provide observables via `of(...)`

Example:

```ts
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

await TestBed.configureTestingModule({
  imports: [ComponentUnderTest],
  providers: [
    provideRouter([]),
    { provide: SomeService, useValue: jasmine.createSpyObj('SomeService', [], { data$: of(null) }) },
  ],
}).compileComponents();

const fixture = TestBed.createComponent(ComponentUnderTest);
fixture.detectChanges();