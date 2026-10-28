import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import {GeneralInformationComponent} from '@app/auth/components/profile/general-user-info/general-user-info.component';
import {ChangePasswordComponent} from '@app/auth/components/profile/change-password/change-password.component';


export type ProfileTab = 'info' | 'password';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [GeneralInformationComponent, ChangePasswordComponent],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfileComponent {
  readonly selectedTab = signal<ProfileTab>('info');

  selectTab(tab: ProfileTab): void {
    this.selectedTab.set(tab);
  }
}
