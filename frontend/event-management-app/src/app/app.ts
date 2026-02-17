import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from './layout/header/header';
import { ToastComponent } from './shared/components/toast/toast.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, HeaderComponent, ToastComponent],
  template: `
    <div class="min-h-screen bg-gray-50">
      <app-header />
      <main>
        <router-outlet />
      </main>
      <app-toast />
    </div>
  `
})
export class AppComponent {
  title = 'Event Management System';
}