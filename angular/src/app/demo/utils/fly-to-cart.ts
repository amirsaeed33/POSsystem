/** Keep in sync with `.fp-fly-to-cart-ball` animation duration in styles.scss */
const FLY_DURATION_MS = 1450;

export interface FlyToCartOptions {
    /** Prefer cloning this product <img> for the flying element. */
    imageEl?: HTMLImageElement | Element | null;
    /** Fallback if no image element is available. */
    imageUrl?: string;
    targetSelector?: string;
    /**
     * When true: do not open/close a cart mouth or swap icons.
     * Only glow/pulse the existing target (used by POS Order Details tab).
     */
    highlightOnly?: boolean;
}

/**
 * Visual-only "fly to cart" animation: clones the product image and arcs it
 * into the cart target. By default the cart mouth opens/closes; with
 * highlightOnly the target icon is left unchanged and only pulsed.
 */
export function flyToCart(
    source?: Event | EventTarget | Element | null,
    options?: FlyToCartOptions
): void {
    if (typeof document === 'undefined' || typeof window === 'undefined') {
        return;
    }

    const startEl = resolveStartElement(source);
    const imageEl = resolveProductImage(options?.imageEl, startEl);

    const origin =
        imageEl ||
        (startEl?.closest?.(
            'button, .btn-add-cart, .btn-submit-cart, .pos-menu-tile, .fp-food-card, .fp-product-card, .product-card, .deal-card, article, .card-media-box'
        ) as HTMLElement | null) ||
        (startEl as HTMLElement | null);

    const startRect = (imageEl || origin)?.getBoundingClientRect?.();
    const targetSelector = options?.targetSelector || '#storeCartFlyTarget';
    const highlightOnly = !!options?.highlightOnly;
    const target =
        document.querySelector(targetSelector) ||
        document.querySelector('.store-cart-fab') ||
        document.querySelector('.fp-cart-trigger');

    let endX: number;
    let endY: number;
    if (target) {
        const r = target.getBoundingClientRect();
        endX = r.left + r.width / 2;
        endY = r.top + r.height / 2;
    } else {
        endX = window.innerWidth - 48;
        endY = window.innerHeight - (window.innerWidth < 768 ? 120 : 56);
    }

    const startX = startRect ? startRect.left + startRect.width / 2 : endX - 80;
    const startY = startRect ? startRect.top + startRect.height / 2 : endY - 80;
    const startSize = startRect
        ? Math.max(36, Math.min(72, Math.min(startRect.width, startRect.height) * 0.55))
        : 48;

    const dx = endX - startX;
    const dy = endY - startY;
    const arcLift = Math.max(72, Math.min(160, Math.abs(dx) * 0.28 + Math.abs(dy) * 0.12));

    const flyer = createFlyer(imageEl, options?.imageUrl, startSize);
    flyer.style.left = `${startX}px`;
    flyer.style.top = `${startY}px`;
    flyer.style.width = `${startSize}px`;
    flyer.style.height = `${startSize}px`;
    flyer.style.setProperty('--fp-dx', `${dx}px`);
    flyer.style.setProperty('--fp-dy', `${dy}px`);
    flyer.style.setProperty('--fp-arc', `${arcLift}px`);
    document.body.appendChild(flyer);

    requestAnimationFrame(() => {
        flyer.classList.add('is-flying');
    });

    // Start highlight early so the destination is obvious while the image travels.
    if (highlightOnly) {
        highlightCartTarget(targetSelector);
    }

    const openTimer = window.setTimeout(() => {
        if (!highlightOnly) {
            setCartMouthState(targetSelector, 'open');
        }
    }, Math.round(FLY_DURATION_MS * 0.55));

    let finished = false;
    const finish = () => {
        if (finished) {
            return;
        }
        finished = true;
        window.clearTimeout(openTimer);
        if (flyer.parentNode) {
            flyer.parentNode.removeChild(flyer);
        }
        if (highlightOnly) {
            highlightCartTarget(targetSelector);
        } else {
            setCartMouthState(targetSelector, 'close');
            pulseCartTarget(targetSelector);
        }
    };

    flyer.addEventListener('animationend', finish, { once: true });
    window.setTimeout(finish, FLY_DURATION_MS + 120);
}

/** Find the product <img> nearest to a click / card. */
export function findProductImageNear(
    source?: Event | EventTarget | Element | null
): HTMLImageElement | null {
    return resolveProductImage(null, resolveStartElement(source));
}

function resolveStartElement(
    source?: Event | EventTarget | Element | null
): Element | null {
    if (source instanceof Element) {
        return source;
    }
    if (source instanceof Event) {
        const t = source.target;
        return t instanceof Element ? t : null;
    }
    if (source && typeof (source as EventTarget).addEventListener === 'function') {
        return source instanceof Node && source.nodeType === 1 ? (source as Element) : null;
    }
    return null;
}

function resolveProductImage(
    preferred?: HTMLImageElement | Element | null,
    near?: Element | null
): HTMLImageElement | null {
    if (preferred instanceof HTMLImageElement && preferred.src) {
        return preferred;
    }
    if (preferred instanceof Element) {
        const nested = preferred.querySelector('img');
        if (nested instanceof HTMLImageElement && nested.src) {
            return nested;
        }
    }

    const scope =
        (near?.closest?.(
            '.pos-menu-tile, .fp-food-card, .fp-product-card, .product-card, .deal-card, .showcase-pocket-content, .card-media-box, article, .p-dialog, .surface-card'
        ) as HTMLElement | null) || near;

    if (!scope) {
        return null;
    }

    const img =
        scope.querySelector<HTMLImageElement>('img.card-img, img.hero-img, img.bundle-img, img') ||
        null;
    return img?.src ? img : null;
}

function createFlyer(
    imageEl: HTMLImageElement | null,
    imageUrl: string | undefined,
    size: number
): HTMLElement {
    if (imageEl?.src) {
        const clone = imageEl.cloneNode(true) as HTMLImageElement;
        clone.removeAttribute('id');
        clone.className = 'fp-fly-to-cart-ball is-product-image';
        clone.alt = '';
        clone.setAttribute('aria-hidden', 'true');
        clone.draggable = false;
        clone.style.width = `${size}px`;
        clone.style.height = `${size}px`;
        return clone;
    }

    const ball = document.createElement('div');
    ball.className = 'fp-fly-to-cart-ball';
    ball.setAttribute('aria-hidden', 'true');
    if (imageUrl) {
        ball.classList.add('has-image');
        ball.style.backgroundImage = `url("${imageUrl.replace(/"/g, '\\"')}")`;
    }
    return ball;
}

function cartMouthTargets(targetSelector: string): Element[] {
    const nodes = [
        document.querySelector('.store-cart-fab'),
        document.querySelector(targetSelector),
        document.querySelector('#storeCartFlyTarget'),
        document.querySelector('.fp-cart-trigger'),
    ].filter(Boolean) as Element[];
    return Array.from(new Set(nodes));
}

function setCartMouthState(targetSelector: string, state: 'open' | 'close'): void {
    const apply = () => {
        for (const el of cartMouthTargets(targetSelector)) {
            // Skip targets with no mouth markup (e.g. POS Order Details tab).
            if (!el.querySelector('.fp-cart-mouth')) {
                continue;
            }
            if (state === 'open') {
                el.classList.add('fp-cart-mouth-open');
                el.classList.remove('fp-cart-mouth-close');
            } else {
                el.classList.remove('fp-cart-mouth-open');
                el.classList.add('fp-cart-mouth-close');
                window.setTimeout(() => el.classList.remove('fp-cart-mouth-close'), 450);
            }
        }
    };
    apply();
    if (state === 'open') {
        window.setTimeout(apply, 40);
    }
}

function pulseCartTarget(targetSelector: string): void {
    for (const el of cartMouthTargets(targetSelector)) {
        el.classList.remove('fp-cart-pop');
        void (el as HTMLElement).offsetWidth;
        el.classList.add('fp-cart-pop');
        window.setTimeout(() => el.classList.remove('fp-cart-pop'), 700);
    }
}

/** Brief glow / border pulse — keeps the existing icon unchanged. */
function highlightCartTarget(targetSelector: string): void {
    const el =
        document.querySelector(targetSelector) ||
        document.querySelector('#storeCartFlyTarget');
    if (!el) {
        return;
    }
    el.classList.remove('fp-cart-receive-glow');
    void (el as HTMLElement).offsetWidth;
    el.classList.add('fp-cart-receive-glow');
    window.setTimeout(() => el.classList.remove('fp-cart-receive-glow'), 900);
}
