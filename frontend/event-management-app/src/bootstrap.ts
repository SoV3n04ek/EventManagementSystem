import { enableProdMode } from '@angular/core';
import { platformBrowser } from '@angular/platform-browser';
import { AppComponent } from './app/app';
import { environment } from './environments/environment';

if (environment.production) {
  enableProdMode();
}

platformBrowser()
  .bootstrapModule(AppComponent)
  .catch(err => console.error(err));
