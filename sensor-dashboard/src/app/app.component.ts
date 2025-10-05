import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  template: `<router-outlet></router-outlet>`,
  styles: [
    `:host { display: block; min-height: 100vh; background: #0f172a; color: #e2e8f0; font-family: 'Inter', sans-serif; }`
  ]
})
export class AppComponent {}
