// Response shapes from contracts/http-api.md. The client never evaluates rules itself
// (constitution Principle II); it displays these server results.

export type RequestStatus = 'Pending' | 'Approved' | 'Denied';

export type BlockingReasonCode = 'TOO_EARLY' | 'NO_REFILLS_REMAINING' | 'INVALID_PRESCRIPTION_DATA';

export type WarningCode = 'ALLERGY_CONFLICT' | 'DRUG_INTERACTION';

export interface BlockingReason {
  code: BlockingReasonCode;
  message: string;
  eligibleAt: string | null;
}

export interface Warning {
  code: WarningCode;
  message: string;
  medicationName: string;
  description: string | null;
}

export interface Evaluation {
  evaluatedAt: string;
  canApprove: boolean;
  eligibleAt: string | null;
  blockingReasons: BlockingReason[];
  warnings: Warning[];
}

export interface RefillRequestSummary {
  id: number;
  status: RequestStatus;
  requestedAt: string;
  decidedAt: string | null;
  patientName: string;
  medicationName: string;
  canApprove: boolean | null;
  hasWarnings: boolean | null;
}

/** Whole-status Pending counts computed by the server (003 contracts/http-api.md). */
export interface ListSummary {
  blocked: number;
  warnings: number;
  clear: number;
}

/** A page request; `search` is sent only when it has text (003 contracts/http-api.md). */
export interface ListQuery {
  page: number;
  pageSize: number;
  search?: string;
}

export interface RefillRequestList {
  evaluatedAt: string;
  items: RefillRequestSummary[];
  /** Paged responses only (003): absent when the list was requested without paging. */
  page?: number;
  pageSize?: number;
  matchingCount?: number;
  statusTotal?: number;
  /** Pending only. */
  summary?: ListSummary;
}

export interface PatientProfile {
  id: number;
  fullName: string;
  dateOfBirth: string;
  allergies: string[];
}

export interface RequestedPrescription {
  id: number;
  medicationName: string;
  dosage: string;
  daysSupply: number;
  remainingRefills: number;
  lastFilledAt: string;
  isActive: boolean;
}

export interface ActivePrescription {
  id: number;
  medicationName: string;
  dosage: string;
  remainingRefills: number;
  lastFilledAt: string;
}

export interface RefillRequestDetail {
  id: number;
  status: RequestStatus;
  requestedAt: string;
  decidedAt: string | null;
  decisionNote: string | null;
  patient: PatientProfile;
  prescription: RequestedPrescription;
  activePrescriptions: ActivePrescription[];
  /** Present only while Pending. */
  evaluation?: Evaluation;
}

export type DecisionKind = 'approve' | 'deny';
