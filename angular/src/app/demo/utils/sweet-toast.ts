/**
 * Warm, unique toast copy for create / update / delete across SmartPos entities.
 * Pick a random summary + entity-specific detail so repeats feel fresh.
 */

export type SweetToastAction = 'create' | 'update' | 'delete' | 'createAnother';

export type SweetToastEntity =
    | 'product'
    | 'category'
    | 'brand'
    | 'unit'
    | 'customer'
    | 'supplier'
    | 'staff'
    | 'department'
    | 'branch'
    | 'deal'
    | 'sale'
    | 'purchase'
    | 'expense'
    | 'account'
    | 'ledger'
    | 'attendance'
    | 'payroll'
    | 'stockAdjustment'
    | 'saleReturn'
    | 'purchaseReturn'
    | 'customerOrder'
    | 'deliveryArea'
    | 'user'
    | 'role'
    | 'tenant'
    | 'lookup'
    | 'emailTemplate'
    | 'package'
    | 'seller'
    | 'developer'
    | 'modifier'
    | 'generic';

export interface SweetToastMessage {
    severity: 'success' | 'info';
    /** Single speech-bubble line (e.g. "Sweet! Your product just joined…"). */
    summary: string;
    detail: string;
    life: number;
    styleClass: string;
}

const SUMMARIES_CREATE = [
    'Sweet!',
    'Lovely!',
    'All set!',
    'Perfect!',
    'Nice one!',
    'Looking good!',
    'Freshly saved!',
    'A little magic!',
];

const SUMMARIES_UPDATE = [
    'Updated!',
    'Polished!',
    'Refined!',
    'Nicely tuned!',
    'Fresh edit!',
    'Looking sharper!',
    'Saved with care!',
    'All freshened!',
];

const SUMMARIES_DELETE = [
    'Cleared!',
    'Tidy!',
    'Removed!',
    'All clean!',
    'Out of the way!',
    'Space made!',
];

const DETAILS: Record<
    SweetToastEntity,
    { create: string[]; update: string[]; delete: string[]; createAnother?: string[] }
> = {
    product: {
        create: [
            'Your product just joined the shelf — ready to shine.',
            'New product is live. Customers will love this.',
            'Catalog just got a little richer.',
        ],
        update: [
            'Product details refreshed — looking sharp.',
            'Those tweaks are saved. Shelf stays perfect.',
            'Product polish complete.',
        ],
        delete: [
            'Product stepped off the shelf.',
            'That item is gone — catalog stays neat.',
        ],
        createAnother: [
            'Saved! Canvas is clear for the next product.',
            'One down — ready for another beauty.',
        ],
    },
    category: {
        create: [
            'New category opened — shelves have a home.',
            'Category ready. Sorting just got sweeter.',
        ],
        update: [
            'Category refreshed — everything stays organized.',
            'Category name and vibe updated.',
        ],
        delete: ['Category cleared from the list.'],
    },
    brand: {
        create: [
            'Brand badge pinned — looking premium.',
            'New brand joined your lineup.',
        ],
        update: [
            'Brand details polished.',
            'Brand update saved with style.',
        ],
        delete: ['Brand removed from the roster.'],
    },
    unit: {
        create: ['Unit of measure ready — counting stays accurate.'],
        update: ['Unit settings updated cleanly.'],
        delete: ['Unit removed.'],
    },
    customer: {
        create: [
            'Welcome aboard — new customer is in the book.',
            'Customer profile ready for lovely service.',
        ],
        update: [
            'Customer details freshened up.',
            'Profile updated — relationship stays warm.',
        ],
        delete: ['Customer removed from the book.'],
    },
    supplier: {
        create: ['Supplier linked — supply chain stays friendly.'],
        update: ['Supplier info updated smoothly.'],
        delete: ['Supplier connection closed.'],
    },
    staff: {
        create: [
            'New teammate added — welcome to the crew.',
            'Staff profile ready to clock in.',
        ],
        update: ['Staff details updated with care.'],
        delete: ['Staff member removed from the roster.'],
    },
    department: {
        create: ['Department created — teams have a home.'],
        update: ['Department refreshed.'],
        delete: ['Department cleared.'],
    },
    branch: {
        create: [
            'Branch planted — awaiting a little host magic to bloom.',
            'New branch is on the map.',
        ],
        update: [
            'Branch details updated — storefront stays bright.',
            'Branch settings saved smoothly.',
        ],
        delete: ['Branch removed.'],
    },
    deal: {
        create: [
            'Deal is live — customers will smile.',
            'Bundle ready. Sweet savings unlocked.',
        ],
        update: [
            'Deal refreshed — offer stays irresistible.',
            'Deal tweaks saved.',
        ],
        delete: ['Deal taken off the board.'],
    },
    sale: {
        create: [
            'Sale sealed — cha-ching with style.',
            'Transaction saved. Great sell!',
        ],
        update: ['Sale details updated.'],
        delete: ['Sale cleared.'],
    },
    purchase: {
        create: [
            'Purchase logged — stock is on its way to happiness.',
            'Buying record saved cleanly.',
        ],
        update: ['Purchase updated.'],
        delete: ['Purchase removed.'],
    },
    expense: {
        create: ['Expense noted — books stay honest.'],
        update: ['Expense updated carefully.'],
        delete: ['Expense cleared from the books.'],
    },
    account: {
        create: ['Account opened — money has a cozy home.'],
        update: ['Account details updated.'],
        delete: ['Account closed out.'],
    },
    ledger: {
        create: ['Ledger line written — books stay balanced.'],
        update: ['Ledger entry polished.'],
        delete: ['Ledger line removed.'],
    },
    attendance: {
        create: ['Attendance marked — day starts right.'],
        update: ['Attendance adjusted gently.'],
        delete: ['Attendance record cleared.'],
    },
    payroll: {
        create: ['Payroll ready — paydays stay sweet.'],
        update: ['Payroll numbers freshened.'],
        delete: ['Payroll entry removed.'],
    },
    stockAdjustment: {
        create: ['Stock adjusted — inventory stays true.'],
        update: ['Stock adjustment updated.'],
        delete: ['Stock adjustment cleared.'],
    },
    saleReturn: {
        create: ['Return recorded — customer care first.'],
        update: ['Sale return updated.'],
        delete: ['Sale return cleared.'],
    },
    purchaseReturn: {
        create: ['Purchase return logged — suppliers stay in sync.'],
        update: ['Purchase return updated.'],
        delete: ['Purchase return cleared.'],
    },
    customerOrder: {
        create: [
            'Order captured — kitchen / counter can smile.',
            'Customer order is in — let’s make it lovely.',
        ],
        update: ['Order details updated.'],
        delete: ['Order removed.'],
    },
    deliveryArea: {
        create: ['Delivery zone drawn — riders know the path.'],
        update: ['Delivery area refreshed.'],
        delete: ['Delivery area cleared.'],
    },
    user: {
        create: ['User invited in — welcome to SmartPos.'],
        update: ['User profile polished.'],
        delete: ['User removed.'],
    },
    role: {
        create: ['Role crafted — permissions with purpose.'],
        update: ['Role permissions refreshed.'],
        delete: ['Role removed.'],
    },
    tenant: {
        create: ['Tenant space opened — business can bloom.'],
        update: ['Tenant settings updated.'],
        delete: ['Tenant removed.'],
    },
    lookup: {
        create: ['Lookup value added — lists stay handy.'],
        update: ['Lookup value updated.'],
        delete: ['Lookup value cleared.'],
    },
    emailTemplate: {
        create: ['Email template ready — messages with heart.'],
        update: ['Email template polished.'],
        delete: ['Email template removed.'],
    },
    package: {
        create: ['Package published — plans look tempting.'],
        update: ['Package details updated.'],
        delete: ['Package removed.'],
    },
    seller: {
        create: ['Seller added — sales team grows.'],
        update: ['Seller profile updated.'],
        delete: ['Seller removed.'],
    },
    developer: {
        create: ['Developer profile created.'],
        update: ['Developer profile updated.'],
        delete: ['Developer removed.'],
    },
    modifier: {
        create: ['Modifier option added — choices feel delicious.'],
        update: ['Modifier option polished.'],
        delete: ['Modifier option cleared.'],
    },
    generic: {
        create: ['Saved beautifully.'],
        update: ['Changes saved with care.'],
        delete: ['Removed cleanly.'],
        createAnother: ['Saved — ready for the next one.'],
    },
};

function pick<T>(list: T[]): T {
    return list[Math.floor(Math.random() * list.length)];
}

function withName(detail: string, name?: string | null): string {
    const n = (name || '').trim();
    if (!n) {
        return detail;
    }
    return `${detail} · “${n}”`;
}

/** Build a unique sweet toast for create / update / delete. */
export function sweetToast(
    entity: SweetToastEntity,
    action: SweetToastAction,
    name?: string | null
): SweetToastMessage {
    const pack = DETAILS[entity] || DETAILS.generic;
    const key: 'create' | 'update' | 'delete' | 'createAnother' =
        action === 'createAnother' ? 'createAnother' : action;

    let detailPool =
        key === 'createAnother'
            ? pack.createAnother || pack.create
            : pack[key] || DETAILS.generic[key] || DETAILS.generic.create;

    if (!detailPool?.length) {
        detailPool = DETAILS.generic.create;
    }

    const headline =
        action === 'delete'
            ? pick(SUMMARIES_DELETE)
            : action === 'update'
              ? pick(SUMMARIES_UPDATE)
              : pick(SUMMARIES_CREATE);

    const body = withName(pick(detailPool), name);

    return {
        severity: action === 'delete' ? 'info' : 'success',
        // One glossy bubble line — matches the speech-pill toast look.
        summary: `${headline} ${body}`,
        detail: '',
        life: 4200,
        styleClass: 'sp-sweet-bubble',
    };
}

/** Convenience: spread into messageService.add({ ...sweetToast(...) }). */
export function sweetSuccess(
    entity: SweetToastEntity,
    action: SweetToastAction,
    name?: string | null
): SweetToastMessage {
    return sweetToast(entity, action, name);
}
