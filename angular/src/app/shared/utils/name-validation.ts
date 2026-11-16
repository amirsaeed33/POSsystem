/**
 * Shared name rules for settings entities (unit, category, etc.).
 * - trimmed length >= 2
 * - must include at least one letter (not symbols-only)
 */
export function validateEntityName(
    value: string | null | undefined,
    label = 'Name'
): string | null {
    const name = (value || '').trim();
    if (name.length < 2) {
        return `${label} must be at least 2 characters.`;
    }
    if (!/[\p{L}]/u.test(name)) {
        return `${label} must contain at least one letter (not only special characters).`;
    }
    return null;
}

export function isEntityNameValid(value: string | null | undefined): boolean {
    return validateEntityName(value) === null;
}
