import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { ConnectionIndicatorComponent } from './shared/ui/connection-indicator/connection-indicator.component';
import { AmbientLightDirective } from './shared/ambient/ambient-light.directive';
import { IconComponent } from './shared/ui/icon.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, ConnectionIndicatorComponent, IconComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // One shared pointer light for the whole app (004 research R4).
  hostDirectives: [AmbientLightDirective],
})
export class AppComponent {}
