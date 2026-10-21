import type { en } from './en';

/**
 * Every interface string, as a typed tree (spec 005 FR-017, research R9). It is the shape of the
 * English dictionary, so a key missing from, or added only to, the Arabic dictionary fails to
 * compile, and parameterized entries keep their parameter types.
 */
export type Messages = typeof en;
