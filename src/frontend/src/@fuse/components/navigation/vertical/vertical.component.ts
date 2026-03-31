import {
    animate,
    AnimationBuilder,
    AnimationPlayer,
    style,
} from '@angular/animations';
import { BooleanInput, coerceBooleanProperty } from '@angular/cdk/coercion';
import { ScrollStrategy, ScrollStrategyOptions } from '@angular/cdk/overlay';
import { DOCUMENT } from '@angular/common';
import {
    AfterViewInit,
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    ElementRef,
    EventEmitter,
    HostBinding,
    HostListener,
    inject,
    Input,
    OnChanges,
    OnDestroy,
    OnInit,
    Output,
    QueryList,
    Renderer2,
    SimpleChanges,
    ViewChild,
    ViewChildren,
    ViewEncapsulation,
} from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { fuseAnimations } from '@fuse/animations';
import { FuseNavigationService } from '@fuse/components/navigation/navigation.service';
import {
    FuseNavigationItem,
    FuseVerticalNavigationAppearance,
    FuseVerticalNavigationMode,
    FuseVerticalNavigationPosition,
} from '@fuse/components/navigation/navigation.types';
import { FuseVerticalNavigationAsideItemComponent } from '@fuse/components/navigation/vertical/components/aside/aside.component';
import { FuseVerticalNavigationBasicItemComponent } from '@fuse/components/navigation/vertical/components/basic/basic.component';
import { FuseVerticalNavigationCollapsableItemComponent } from '@fuse/components/navigation/vertical/components/collapsable/collapsable.component';
import { FuseVerticalNavigationDividerItemComponent } from '@fuse/components/navigation/vertical/components/divider/divider.component';
import { FuseVerticalNavigationGroupItemComponent } from '@fuse/components/navigation/vertical/components/group/group.component';
import { FuseVerticalNavigationSpacerItemComponent } from '@fuse/components/navigation/vertical/components/spacer/spacer.component';
import { FuseScrollbarDirective } from '@fuse/directives/scrollbar/scrollbar.directive';
import { FuseUtilsService } from '@fuse/services/utils/utils.service';
import {
    delay,
    filter,
    merge,
    ReplaySubject,
    Subject,
    Subscription,
    takeUntil,
} from 'rxjs';

@Component({
    selector: 'fuse-vertical-navigation',
    templateUrl: './vertical.component.html',
    styleUrls: ['./vertical.component.scss'],
    animations: fuseAnimations,
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush,
    exportAs: 'fuseVerticalNavigation',
    imports: [
        FuseScrollbarDirective,
        FuseVerticalNavigationAsideItemComponent,
        FuseVerticalNavigationBasicItemComponent,
        FuseVerticalNavigationCollapsableItemComponent,
        FuseVerticalNavigationDividerItemComponent,
        FuseVerticalNavigationGroupItemComponent,
        FuseVerticalNavigationSpacerItemComponent,
    ],
})
export class FuseVerticalNavigationComponent
    implements OnChanges, OnInit, AfterViewInit, OnDestroy
{
    /* eslint-disable @typescript-eslint/naming-convention */
    static ngAcceptInputType_inner: BooleanInput;
    static ngAcceptInputType_opened: BooleanInput;
    static ngAcceptInputType_transparentOverlay: BooleanInput;
    /* eslint-enable @typescript-eslint/naming-convention */

    private _animationBuilder = inject(AnimationBuilder);
    private _changeDetectorRef = inject(ChangeDetectorRef);
    private _document = inject(DOCUMENT);
    private _elementRef = inject(ElementRef);
    private _renderer2 = inject(Renderer2);
    private _router = inject(Router);
    private _scrollStrategyOptions = inject(ScrollStrategyOptions);
    private _fuseNavigationService = inject(FuseNavigationService);
    private _fuseUtilsService = inject(FuseUtilsService);

    @Input() appearance: FuseVerticalNavigationAppearance = 'default';
    @Input() autoCollapse: boolean = true;
    @Input() inner: boolean = false;
    @Input() mode: FuseVerticalNavigationMode = 'side';
    @Input() name: string = this._fuseUtilsService.randomId();
    @Input() navigation: FuseNavigationItem[];
    @Input() opened: boolean = true;
    @Input() position: FuseVerticalNavigationPosition = 'left';
    @Input() transparentOverlay: boolean = false;
    @Output()
    readonly appearanceChanged: EventEmitter<FuseVerticalNavigationAppearance> =
        new EventEmitter<FuseVerticalNavigationAppearance>();
    @Output() readonly modeChanged: EventEmitter<FuseVerticalNavigationMode> =
        new EventEmitter<FuseVerticalNavigationMode>();
    @Output() readonly openedChanged: EventEmitter<boolean> =
        new EventEmitter<boolean>();
    @Output()
    readonly positionChanged: EventEmitter<FuseVerticalNavigationPosition> =
        new EventEmitter<FuseVerticalNavigationPosition>();
    @ViewChild('navigationContent') private _navigationContentEl: ElementRef;

    // Public properties for thin appearance
    activeAsideItemId: string | null = null;
    showNormalSidebar: boolean = true;

    onCollapsableItemCollapsed: ReplaySubject<FuseNavigationItem> =
        new ReplaySubject<FuseNavigationItem>(1);
    onCollapsableItemExpanded: ReplaySubject<FuseNavigationItem> =
        new ReplaySubject<FuseNavigationItem>(1);
    onRefreshed: ReplaySubject<boolean> = new ReplaySubject<boolean>(1);
    private _animationsEnabled: boolean = false;
    private _asideOverlay: HTMLElement;
    private readonly _handleAsideOverlayClick: any;
    private readonly _handleOverlayClick: any;
    private _hovered: boolean = false;
    private _mutationObserver: MutationObserver;
    private _overlay: HTMLElement;
    private _player: AnimationPlayer;
    private _scrollStrategy: ScrollStrategy =
        this._scrollStrategyOptions.block();
    private _fuseScrollbarDirectives!: QueryList<FuseScrollbarDirective>;
    private _fuseScrollbarDirectivesSubscription: Subscription;
    private _unsubscribeAll: Subject<any> = new Subject<any>();

    /**
     * Constructor
     */
    constructor() {
        this._handleAsideOverlayClick = (): void => {
            this.closeAside();
        };
        this._handleOverlayClick = (): void => {
            this.close();
        };
    }

    // -----------------------------------------------------------------------------------------------------
    // @ Accessors
    // -----------------------------------------------------------------------------------------------------

    /**
     * Host binding for component classes
     */
    @HostBinding('class') get classList(): any {
        /* eslint-disable @typescript-eslint/naming-convention */
        return {
            'fuse-vertical-navigation-animations-enabled':
                this._animationsEnabled,
            [`fuse-vertical-navigation-appearance-${this.appearance}`]: true,
            'fuse-vertical-navigation-hover': this._hovered,
            'fuse-vertical-navigation-inner': this.inner,
            'fuse-vertical-navigation-mode-over': this.mode === 'over',
            'fuse-vertical-navigation-mode-side': this.mode === 'side',
            'fuse-vertical-navigation-opened': this.opened,
            'fuse-vertical-navigation-position-left': this.position === 'left',
            'fuse-vertical-navigation-position-right':
                this.position === 'right',
        };
        /* eslint-enable @typescript-eslint/naming-convention */
    }

    /**
     * Host binding for component inline styles
     */
    @HostBinding('style') get styleList(): any {
        return {
            visibility: this.opened ? 'visible' : 'hidden',
        };
    }

    /**
     * Setter for fuseScrollbarDirectives
     */
    @ViewChildren(FuseScrollbarDirective)
    set fuseScrollbarDirectives(
        fuseScrollbarDirectives: QueryList<FuseScrollbarDirective>
    ) {
        // Store the directives
        this._fuseScrollbarDirectives = fuseScrollbarDirectives;

        // Return if there are no directives
        if (fuseScrollbarDirectives.length === 0) {
            return;
        }

        // Unsubscribe the previous subscriptions
        if (this._fuseScrollbarDirectivesSubscription) {
            this._fuseScrollbarDirectivesSubscription.unsubscribe();
        }

        // Update the scrollbars on collapsable items' collapse/expand
        this._fuseScrollbarDirectivesSubscription = merge(
            this.onCollapsableItemCollapsed,
            this.onCollapsableItemExpanded
        )
            .pipe(takeUntil(this._unsubscribeAll), delay(250))
            .subscribe(() => {
                // Loop through the scrollbars and update them
                fuseScrollbarDirectives.forEach((fuseScrollbarDirective) => {
                    fuseScrollbarDirective.update();
                });
            });
    }

    // -----------------------------------------------------------------------------------------------------
    // @ Decorated methods
    // -----------------------------------------------------------------------------------------------------

    /**
     * On mouseenter
     *
     * @private
     */
    @HostListener('mouseenter')
    private _onMouseenter(): void {
        // Enable the animations
        this._enableAnimations();

        // Set the hovered
        this._hovered = true;
    }

    /**
     * On mouseleave
     *
     * @private
     */
    @HostListener('mouseleave')
    private _onMouseleave(): void {
        // Enable the animations
        this._enableAnimations();

        // Set the hovered
        this._hovered = false;
    }

    // -----------------------------------------------------------------------------------------------------
    // @ Lifecycle hooks
    // -----------------------------------------------------------------------------------------------------

    /**
     * On changes
     *
     * @param changes
     */
    ngOnChanges(changes: SimpleChanges): void {
        // Appearance
        if ('appearance' in changes) {
            // Execute the observable
            this.appearanceChanged.next(changes.appearance.currentValue);
        }

        // Inner
        if ('inner' in changes) {
            // Coerce the value to a boolean
            this.inner = coerceBooleanProperty(changes.inner.currentValue);
        }

        // Mode
        if ('mode' in changes) {
            // Get the previous and current values
            const currentMode = changes.mode.currentValue;
            const previousMode = changes.mode.previousValue;

            // Disable the animations
            this._disableAnimations();

            // If the mode changes: 'over -> side'
            if (previousMode === 'over' && currentMode === 'side') {
                // Hide the overlay
                this._hideOverlay();
            }

            // If the mode changes: 'side -> over'
            if (previousMode === 'side' && currentMode === 'over') {
                // Close the aside
                this.closeAside();

                // If the navigation is opened
                if (this.opened) {
                    // Show the overlay
                    this._showOverlay();
                }
            }

            // Execute the observable
            this.modeChanged.next(currentMode);

            // Enable the animations after a delay
            setTimeout(() => {
                this._enableAnimations();
            }, 500);

            // Ensure active aside sync when mode switches (desktop <-> mobile)
            this._updateActiveAsideItemFromUrl();
        }

        // Navigation
        if ('navigation' in changes) {
            this._updateActiveAsideItemFromUrl();
            // Mark for check
            this._changeDetectorRef.markForCheck();
        }

        // Opened
        if ('opened' in changes) {
            // Coerce the value to a boolean
            this.opened = coerceBooleanProperty(changes.opened.currentValue);

            // Open/close the navigation
            this._toggleOpened(this.opened);

            if (this.opened) {
                this._updateActiveAsideItemFromUrl();
            }
        }

        // Position
        if ('position' in changes) {
            // Execute the observable
            this.positionChanged.next(changes.position.currentValue);
        }

        // Transparent overlay
        if ('transparentOverlay' in changes) {
            // Coerce the value to a boolean
            this.transparentOverlay = coerceBooleanProperty(
                changes.transparentOverlay.currentValue
            );
        }
    }

    /**
     * On init
     */
    ngOnInit(): void {
        // Make sure the name input is not an empty string
        if (this.name === '') {
            this.name = this._fuseUtilsService.randomId();
        }

        // Register the navigation component
        this._fuseNavigationService.registerComponent(this.name, this);

        // Subscribe to the 'NavigationEnd' event
        this._router.events
            .pipe(
                filter((event) => event instanceof NavigationEnd),
                takeUntil(this._unsubscribeAll)
            )
            .subscribe(() => {
                // Update active aside item based on URL
                this._updateActiveAsideItemFromUrl();

                // DON'T close aside for thin appearance in side mode
                if (
                    this.appearance !== 'thin' &&
                    this.mode === 'side' &&
                    this.activeAsideItemId
                ) {
                    // Close the aside
                    this.closeAside();
                }
            });
    }

    /**
     * After view init
     */
    ngAfterViewInit(): void {
        // Fix for Firefox
        this._mutationObserver = new MutationObserver((mutations) => {
            mutations.forEach((mutation) => {
                const mutationTarget = mutation.target as HTMLElement;
                if (mutation.attributeName === 'class') {
                    if (
                        mutationTarget.classList.contains(
                            'cdk-global-scrollblock'
                        )
                    ) {
                        const top = parseInt(mutationTarget.style.top, 10);
                        this._renderer2.setStyle(
                            this._elementRef.nativeElement,
                            'margin-top',
                            `${Math.abs(top)}px`
                        );
                    } else {
                        this._renderer2.setStyle(
                            this._elementRef.nativeElement,
                            'margin-top',
                            null
                        );
                    }
                }
            });
        });
        this._mutationObserver.observe(this._document.documentElement, {
            attributes: true,
            attributeFilter: ['class'],
        });

        setTimeout(() => {
            // Return if 'navigation content' element does not exist
            if (!this._navigationContentEl) {
                return;
            }

            // Scroll to active item
            if (
                !this._navigationContentEl.nativeElement.classList.contains(
                    'ps'
                )
            ) {
                const activeItem =
                    this._navigationContentEl.nativeElement.querySelector(
                        '.fuse-vertical-navigation-item-active'
                    );

                if (activeItem) {
                    activeItem.scrollIntoView();
                }
            } else {
                this._fuseScrollbarDirectives.forEach(
                    (fuseScrollbarDirective) => {
                        if (!fuseScrollbarDirective.isEnabled()) {
                            return;
                        }

                        fuseScrollbarDirective.scrollToElement(
                            '.fuse-vertical-navigation-item-active',
                            -120,
                            true
                        );
                    }
                );
            }

            // Auto-select aside item for thin appearance
            if (this.appearance === 'thin' && this.mode === 'side') {
                this._updateActiveAsideItemFromUrl();
            }
        });
    }

    /**
     * On destroy
     */
    ngOnDestroy(): void {
        // Disconnect the mutation observer
        this._mutationObserver.disconnect();

        // Forcefully close the navigation and aside
        this.close();
        this.closeAside();

        // Deregister the navigation component
        this._fuseNavigationService.deregisterComponent(this.name);

        // Unsubscribe from all subscriptions
        this._unsubscribeAll.next(null);
        this._unsubscribeAll.complete();
    }

    // -----------------------------------------------------------------------------------------------------
    // @ Public methods
    // -----------------------------------------------------------------------------------------------------

    /**
     * Refresh the component
     */
    refresh(): void {
        // Mark for check
        this._changeDetectorRef.markForCheck();

        // Execute the observable
        this.onRefreshed.next(true);
    }

    /**
     * Open the navigation
     */
    open(): void {
        // For thin appearance in OVER mode, open whole navigation
        if (this.appearance === 'thin' && this.mode === 'over') {
            if (this.opened) {
                return;
            }
            this._toggleOpened(true);
            return;
        }

        // For thin appearance in SIDE mode, open normal sidebar
        if (this.appearance === 'thin' && this.mode === 'side') {
            this.showNormalSidebarPanel();
            return;
        }

        // Default behavior
        if (this.opened) {
            return;
        }

        this._toggleOpened(true);
    }

    /**
     * Close the navigation
     */
    close(): void {
        // For thin appearance in OVER mode, close whole navigation
        if (this.appearance === 'thin' && this.mode === 'over') {
            if (!this.opened) {
                return;
            }
            this._toggleOpened(false);
            return;
        }

        // For thin appearance in SIDE mode, close normal sidebar
        if (this.appearance === 'thin' && this.mode === 'side') {
            this.hideNormalSidebarPanel();
            return;
        }

        // Default behavior
        if (!this.opened) {
            return;
        }

        this.closeAside();
        this._toggleOpened(false);
    }

    /**
     * Toggle the navigation
     */
    toggle(): void {
        // For thin appearance in OVER mode, toggle whole navigation
        if (this.appearance === 'thin' && this.mode === 'over') {
            if (this.opened) {
                this.close();
            } else {
                this.open();
            }
            return;
        }

        // For thin appearance in SIDE mode, toggle normal sidebar
        if (this.appearance === 'thin' && this.mode === 'side') {
            this.toggleNormalSidebar();
            return;
        }

        // Default behavior
        if (this.opened) {
            this.close();
        } else {
            this.open();
        }
    }

    /**
     * Toggle normal sidebar (for thin appearance)
     */
    toggleNormalSidebar(): void {
        console.log('🔄 toggleNormalSidebar:', {
            current: this.showNormalSidebar,
            appearance: this.appearance,
            mode: this.mode,
        });

        this.showNormalSidebar = !this.showNormalSidebar;
        this._changeDetectorRef.detectChanges();

        console.log('✅ New showNormalSidebar:', this.showNormalSidebar);
    }

    /**
     * Show normal sidebar panel
     */
    showNormalSidebarPanel(): void {
        this.showNormalSidebar = true;
        this._changeDetectorRef.detectChanges();
    }

    /**
     * Hide normal sidebar panel
     */
    hideNormalSidebarPanel(): void {
        this.showNormalSidebar = false;
        this._changeDetectorRef.detectChanges();
    }

    /**
     * Open the aside
     */
    openAside(item: FuseNavigationItem): void {
        console.log('🚀 openAside:', item.title);

        // Return if disabled
        if (item.disabled || !item.id) {
            return;
        }

        // Set active
        this.activeAsideItemId = item.id;

        // Show overlay (only for non-thin or over mode)
        if (this.appearance !== 'thin' || this.mode === 'over') {
            this._showAsideOverlay();
        }

        // FORCE change detection
        this._changeDetectorRef.detectChanges();
        this.onRefreshed.next(true);

        console.log('✅ activeAsideItemId set to:', this.activeAsideItemId);
    }

    /**
     * Close the aside
     */
    closeAside(): void {
        // Don't close for thin in side mode
        if (
            (this.appearance === 'thin' || this.appearance === 'compact') &&
            this.mode === 'side'
        ) {
            return;
        }

        this.activeAsideItemId = null;
        this._hideAsideOverlay();
        this._changeDetectorRef.markForCheck();
    }

    /**
     * Toggle the aside
     */
    toggleAside(item: FuseNavigationItem): void {
        console.log(
            '🎯 toggleAside:',
            item.title,
            'current:',
            this.activeAsideItemId
        );

        // Mobile behavior (mode: over): navigate directly to the first child and keep nav open
        if (this.mode === 'over') {
            this.activeAsideItemId = item.id || null;
            this._navigateToFirstChild(item);
            // Keep sidebar open; overlay remains so user can switch sections
            this._changeDetectorRef.markForCheck();
            return;
        }

        // For thin in side mode, switch between items
        if (
            (this.appearance === 'thin' || this.appearance === 'compact') &&
            this.mode === 'side'
        ) {
            if (this.activeAsideItemId === item.id) {
                console.log('Same item, keeping open');
                return;
            } else {
                console.log('Different item, switching');
                this.openAside(item);
                this._navigateToFirstChild(item);
            }
        } else {
            // Default: toggle open/close
            if (this.activeAsideItemId === item.id) {
                this.closeAside();
            } else {
                this.openAside(item);
            }
        }
    }

    /**
     * Track by function
     */
    trackByFn(index: number, item: any): any {
        return item.id || index;
    }

    // -----------------------------------------------------------------------------------------------------
    // @ Private methods
    // -----------------------------------------------------------------------------------------------------

    private _enableAnimations(): void {
        if (this._animationsEnabled) {
            return;
        }
        this._animationsEnabled = true;
    }

    private _disableAnimations(): void {
        if (!this._animationsEnabled) {
            return;
        }
        this._animationsEnabled = false;
    }

    private _showOverlay(): void {
        // Prevent creating multiple main overlays
        if (this._overlay) {
            return;
        }

        this._overlay = this._renderer2.createElement('div');
        this._overlay.classList.add('fuse-vertical-navigation-overlay');

        if (this.transparentOverlay) {
            this._overlay.classList.add(
                'fuse-vertical-navigation-overlay-transparent'
            );
        }

        this._renderer2.appendChild(
            this._elementRef.nativeElement.parentElement,
            this._overlay
        );

        this._scrollStrategy.enable();

        this._player = this._animationBuilder
            .build([
                animate(
                    '300ms cubic-bezier(0.25, 0.8, 0.25, 1)',
                    style({ opacity: 1 })
                ),
            ])
            .create(this._overlay);

        this._player.play();
        this._overlay.addEventListener('click', this._handleOverlayClick);
    }

    private _hideOverlay(): void {
        if (!this._overlay) {
            return;
        }

        this._player = this._animationBuilder
            .build([
                animate(
                    '300ms cubic-bezier(0.25, 0.8, 0.25, 1)',
                    style({ opacity: 0 })
                ),
            ])
            .create(this._overlay);

        this._player.play();

        this._player.onDone(() => {
            if (this._overlay) {
                this._overlay.removeEventListener(
                    'click',
                    this._handleOverlayClick
                );
                this._overlay.parentNode.removeChild(this._overlay);
                this._overlay = null;
            }
            this._scrollStrategy.disable();
        });
    }

    private _showAsideOverlay(): void {
        // Skip for thin in side mode
        if (this.appearance === 'thin' && this.mode === 'side') {
            return;
        }

        if (this._asideOverlay) {
            return;
        }

        this._asideOverlay = this._renderer2.createElement('div');
        this._asideOverlay.classList.add(
            'fuse-vertical-navigation-aside-overlay'
        );

        this._renderer2.appendChild(
            this._elementRef.nativeElement.parentElement,
            this._asideOverlay
        );

        this._player = this._animationBuilder
            .build([
                animate(
                    '300ms cubic-bezier(0.25, 0.8, 0.25, 1)',
                    style({ opacity: 1 })
                ),
            ])
            .create(this._asideOverlay);

        this._player.play();
        this._asideOverlay.addEventListener(
            'click',
            this._handleAsideOverlayClick
        );
    }

    private _hideAsideOverlay(): void {
        if (!this._asideOverlay) {
            return;
        }

        this._player = this._animationBuilder
            .build([
                animate(
                    '300ms cubic-bezier(0.25, 0.8, 0.25, 1)',
                    style({ opacity: 0 })
                ),
            ])
            .create(this._asideOverlay);

        this._player.play();

        this._player.onDone(() => {
            if (this._asideOverlay) {
                this._asideOverlay.removeEventListener(
                    'click',
                    this._handleAsideOverlayClick
                );
                this._asideOverlay.parentNode.removeChild(this._asideOverlay);
                this._asideOverlay = null;
            }
        });
    }

    private _toggleOpened(open: boolean): void {
        this.opened = open;
        this._enableAnimations();

        if (this.mode === 'over') {
            if (this.opened) {
                this._showOverlay();
            } else {
                this._hideOverlay();
            }
        }

        this.openedChanged.next(open);
    }

    private _hasActiveChild(item: FuseNavigationItem, currentUrl: string): boolean {
        const children = item.children;

        if (!children) {
            return false;
        }

        for (const child of children) {
            if (child.children) {
                if (this._hasActiveChild(child, currentUrl)) {
                    return true;
                }
            }

            if (child.link && this._router.isActive(child.link, child.exactMatch || false)) {
                return true;
            }
        }

        return false;
    }

    private _updateActiveAsideItemFromUrl(): void {
        if (
            (this.appearance !== 'thin' && this.appearance !== 'compact') ||
            !this.navigation
        ) {
            return;
        }

        // Find the item that matches the current URL
        let activeItem = this.navigation.find(
            (item) =>
                item.type === 'aside' &&
                this._hasActiveChild(item, this._router.url)
        );

        // If no matching item found, fall back to the first aside item
        if (!activeItem) {
            activeItem = this.navigation.find(
                (item) =>
                    item.type === 'aside' &&
                    (!item.hidden || (item.hidden && !item.hidden(item)))
            );
        }

        if (activeItem) {
            // Only update if changed to prevent unnecessary detection cycles
            if (this.activeAsideItemId !== activeItem.id) {
                this.activeAsideItemId = activeItem.id;
                this._changeDetectorRef.markForCheck();
            }
        }
    }

    private _navigateToFirstChild(item: FuseNavigationItem): void {
        const firstChild = this._findFirstChildWithLink(item);
        if (firstChild && firstChild.link) {
            this._router.navigate([firstChild.link]);
        }
    }

    private _findFirstChildWithLink(item: FuseNavigationItem): FuseNavigationItem | null {
        if (!item.children) {
            return null;
        }

        for (const child of item.children) {
            if (child.type === 'basic' && child.link) {
                return child;
            }
            if (child.children) {
                const found = this._findFirstChildWithLink(child);
                if (found) {
                    return found;
                }
            }
        }
        return null;
    }
}
