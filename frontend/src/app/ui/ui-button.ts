import { Component, input } from '@angular/core';

type Variant = 'primary' | 'secondary' | 'ghost' | 'danger';

/**
 * Reusable button styled with the Organic design tokens.
 * Usage: <button uiButton variant="primary" [block]="true">Save</button>
 */
@Component({
  selector: 'button[uiButton]',
  template: `<ng-content />`,
  host: {
    class: 'btn',
    '[class.btn-primary]': "variant() === 'primary'",
    '[class.btn-secondary]': "variant() === 'secondary'",
    '[class.btn-ghost]': "variant() === 'ghost'",
    '[class.btn-danger]': "variant() === 'danger'",
    '[class.btn-block]': 'block()',
    '[class.btn-sm]': 'size() === "sm"',
  },
})
export class UiButton {
  readonly variant = input<Variant>('secondary');
  readonly block = input(false);
  readonly size = input<'sm' | 'md'>('md');
}
