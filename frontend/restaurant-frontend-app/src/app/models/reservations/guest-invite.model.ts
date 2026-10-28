export interface GuestInvite {
  GroupReservationId: string;
  LocationId: string;
  RestaurantName: string;
  LocationAddress: string;
  StartTime: string;
  EndTime: string;
  MaxGuestCount: number;
  TableNumber: number;
  InviterName: string;
}

export interface GroupInviteToken {
  token: string;
}
