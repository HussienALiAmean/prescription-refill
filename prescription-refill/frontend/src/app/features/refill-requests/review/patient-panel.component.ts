import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
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

  protected readonly dateOfBirth = computed(() => formatDateOnly(this.patient().dateOfBirth));
}
