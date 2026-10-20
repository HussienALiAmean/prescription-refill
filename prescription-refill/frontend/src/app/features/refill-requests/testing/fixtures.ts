import { Evaluation, RefillRequestDetail, RefillRequestList } from '../data/models';

export const EVALUATED_AT = '2026-10-20T11:00:00.1234567+00:00';

export function evaluation(overrides: Partial<Evaluation> = {}): Evaluation {
  return {
    evaluatedAt: EVALUATED_AT,
    canApprove: true,
    eligibleAt: '2026-10-17T08:30:00+00:00',
    blockingReasons: [],
    warnings: [],
    ...overrides,
  };
}

export function blockedEvaluation(): Evaluation {
  return evaluation({
    canApprove: false,
    eligibleAt: '2026-11-08T09:00:00+00:00',
    blockingReasons: [
      {
        code: 'TOO_EARLY',
        message: 'Less than 80% of the 30-day supply has elapsed since the last fill.',
        eligibleAt: '2026-11-08T09:00:00+00:00',
      },
    ],
  });
}

export function detail(overrides: Partial<RefillRequestDetail> = {}): RefillRequestDetail {
  return {
    id: 5,
    status: 'Pending',
    requestedAt: '2026-10-19T16:00:00+00:00',
    decidedAt: null,
    decisionNote: null,
    patient: { id: 5, fullName: 'Sam Patel', dateOfBirth: '1979-11-02', allergies: [] },
    prescription: {
      id: 7,
      medicationName: 'Fenquira',
      dosage: '10 mg by mouth once daily',
      daysSupply: 30,
      remainingRefills: 5,
      lastFilledAt: '2026-09-23T08:30:00+00:00',
      isActive: true,
    },
    activePrescriptions: [
      { id: 8, medicationName: 'Glimora', dosage: '5 mg by mouth once daily', remainingRefills: 2, lastFilledAt: '2026-10-01T08:30:00+00:00' },
    ],
    evaluation: evaluation({
      warnings: [
        {
          code: 'DRUG_INTERACTION',
          message: 'Fenquira has a synthetic demo interaction with active medication Glimora.',
          medicationName: 'Glimora',
          description: 'Synthetic demo interaction (fictional, not clinical guidance).',
        },
      ],
    }),
    ...overrides,
  };
}

export function decided(status: 'Approved' | 'Denied' = 'Approved'): RefillRequestDetail {
  const { evaluation: _omit, ...rest } = detail();
  return { ...rest, status, decidedAt: '2026-10-20T11:30:00+00:00', decisionNote: 'Stable on current dose.' };
}

export function list(overrides: Partial<RefillRequestList> = {}): RefillRequestList {
  return {
    evaluatedAt: EVALUATED_AT,
    items: [
      {
        id: 1,
        status: 'Pending',
        requestedAt: '2026-10-18T07:30:00+00:00',
        decidedAt: null,
        patientName: 'Avery Lindqvist',
        medicationName: 'Avelorin',
        canApprove: true,
        hasWarnings: false,
      },
      {
        id: 6,
        status: 'Pending',
        requestedAt: '2026-10-19T09:00:00+00:00',
        decidedAt: null,
        patientName: 'Taylor Nguyen',
        medicationName: 'Halvexin',
        canApprove: false,
        hasWarnings: true,
      },
    ],
    ...overrides,
  };
}

/**
 * A paged response (003). Totals and summary are given explicitly, never derived from the items'
 * flags, so specs can prove the screen shows the server's numbers.
 */
export function pagedList(overrides: Partial<RefillRequestList> = {}): RefillRequestList {
  const items = overrides.items ?? list().items;
  return {
    ...list(),
    page: 1,
    pageSize: 10,
    matchingCount: items.length,
    statusTotal: items.length,
    summary: { blocked: 1, warnings: 1, clear: 1 },
    ...overrides,
  };
}
