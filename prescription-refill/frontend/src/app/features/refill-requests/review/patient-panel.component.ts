import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { I18nService } from '../../../core/i18n/i18n.service';
import { formatDateOnly } from '../../../shared/time/format-time';
import { InstantComponent } from '../../../shared/time/instant.component';
import { IconComponent } from '../../../shared/ui/icon.component';
import { ActivePrescription, PatientProfile, RequestedPrescription } from '../data/models';

/** Patient profile, allergies, the requested prescription, and other active prescriptions (FR-018). */
@Component({
  selector: 'app-patient-panel',
  imports: [IconComponent, InstantComponent],
  templateUrl: './patient-panel.component.html',
  styleUrl: './review-panels.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PatientPanelComponent {
  readonly patient = input.required<PatientProfile>();
  readonly prescription = input.required<RequestedPrescription>();
  readonly activePrescriptions = input.required<ActivePrescription[]>();

  private readonly i18n = inject(I18nService);
  protected readonly t = this.i18n.t;

  protected readonly dateOfBirth = computed(() => formatDateOnly(this.patient().dateOfBirth, this.i18n.language()));
}
