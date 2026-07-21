import { Component, input, output } from '@angular/core';

/**
 * Modal dialog shell styled with the Organic tokens. Renders a backdrop + panel
 * when [open] is true; projects arbitrary content. Emits (close) on backdrop
 * click or Escape.
 *
 * Usage:
 *   <ui-dialog [open]="showDialog()" (close)="showDialog.set(false)">
 *     <h3 class="dialog-title">Title</h3>
 *     <div class="dialog-body">…</div>
 *     <div class="dialog-actions">…</div>
 *   </ui-dialog>
 */
@Component({
  selector: 'ui-dialog',
  template: `
    @if (open()) {
      <div class="dialog-backdrop" (click)="onBackdrop($event)">
        <div class="dialog elev-lg" role="dialog" aria-modal="true" (keydown.escape)="close.emit()" tabindex="-1">
          <ng-content />
        </div>
      </div>
    }
  `,
})
export class UiDialog {
  readonly open = input(false);
  readonly close = output<void>();

  protected onBackdrop(event: MouseEvent): void {
    if (event.target === event.currentTarget) this.close.emit();
  }
}
