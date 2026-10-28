import { Component, ChangeDetectionStrategy } from '@angular/core';
import { HeaderComponent } from '@app/shared/custom-components/header/header.component';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-main-layout',
  imports: [HeaderComponent, RouterOutlet],
  templateUrl: './main-layout.component.html',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './main-layout.component.scss',
})
export class MainLayoutComponent {}
