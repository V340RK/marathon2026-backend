import { Injectable, signal } from '@angular/core';

export interface LoginDraft {
    email: string;
    password: string;
}

@Injectable({ providedIn: 'root' })
export class LoginDraftService {
    private readonly _draft = signal<LoginDraft>({ email: '', password: '' });

    readonly draft = this._draft.asReadonly();

    set(partial: Partial<LoginDraft>) {
        this._draft.update((d) => ({ ...d, ...partial }));
    }

    clear() {
        this._draft.set({ email: '', password: ''});
    }
}