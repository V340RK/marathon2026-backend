import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';

export interface CreateGuestSessionRequest {
    groupReservationId: string;
    guestName: string;
}

export interface CreateGuestSessionResponse {
    SessionId: string;
    SessionToken: string;
}

export interface GuestMenuDish {
    id: string;
    name: string;
    description: string;
    price: number;
    weight?: string;
    calories?: string;
    proteins?: string;
    fats?: string;
    carbohydrates?: string;
    vitamins?: string;
    imageUrl?: string | null;
    state: string;        // "Available" | "On Stop"
    selectable: boolean;  // true/false
}

export type GuestMenuCategoryKey = string;

export interface GuestMenuResponse {
    dishes: Record<GuestMenuCategoryKey, GuestMenuDish[]>;
}

@Injectable({ providedIn: 'root' })
export class GuestSessionsService {
    private readonly baseUrl = inject(API_BASE_URL);
    private readonly http = inject(HttpClient);

    createSession(body: CreateGuestSessionRequest) {
        return this.http.post<CreateGuestSessionResponse>(`${this.baseUrl}/guest/sessions`, body);
    }

    getMenu(guestSessionId: string, sessionToken: string) {
        return this.http.get<GuestMenuResponse>(
            `${this.baseUrl}/guest/sessions/${guestSessionId}/menu`,
            { headers: this.authHeaders(sessionToken) }
        );
    }

    addDish(guestSessionId: string, sessionToken: string, dishId: string, quantity: number) {
        return this.http.post(
            `${this.baseUrl}/guest/sessions/${guestSessionId}/dishes`,
            { dishId, quantity },
            { headers: this.authHeaders(sessionToken) }
        );
    }

    removeDish(guestSessionId: string, sessionToken: string, dishId: string, quantity: number) {
        return this.http.request(
            'delete',
            `${this.baseUrl}/guest/sessions/${guestSessionId}/dishes`,
            { body: { dishId, quantity }, headers: this.authHeaders(sessionToken) }
        );
    }

    submit(guestSessionId: string, sessionToken: string) {
        return this.http.post(
            `${this.baseUrl}/guest/sessions/${guestSessionId}/submit`,
            {},
            { headers: this.authHeaders(sessionToken) }
        );
    }

    private authHeaders(token: string) {
        return new HttpHeaders({ Authorization: `Bearer ${token}` });
    }
}