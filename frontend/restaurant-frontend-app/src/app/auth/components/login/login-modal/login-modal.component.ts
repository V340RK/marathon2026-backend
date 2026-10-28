import { ChangeDetectionStrategy, Component, HostListener, output } from '@angular/core';
import { LoginFormComponent } from '../login-form/login-form.component'

@Component({
    selector: 'app-login-modal',
    standalone: true,
    imports: [LoginFormComponent],
    templateUrl: './login-modal.component.html',
    styleUrl: './login-modal.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginModalComponent {
    readonly closed = output<void>();

    close() {
        this.closed.emit();
    }

    @HostListener('document:keydown.escape')
    onEsc() {
        this.close();
    }
}