import { animate, style, transition, trigger } from '@angular/animations';
import { Component, OnDestroy, OnInit, ViewEncapsulation } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ActivatedRoute, Router, RouterOutlet } from '@angular/router';
import { fuseAnimations } from '@fuse/animations';
import { FuseFullscreenComponent } from '@fuse/components/fullscreen';
import { FuseLoadingBarComponent } from '@fuse/components/loading-bar';
import {
    FuseNavigationService,
    FuseVerticalNavigationComponent,
} from '@fuse/components/navigation';
import { FuseConfigService, Scheme } from '@fuse/services/config';
import { FuseMediaWatcherService } from '@fuse/services/media-watcher';
import { NavigationService } from 'app/core/navigation/navigation.service';
import { Navigation } from 'app/core/navigation/navigation.types';
import { Subject, takeUntil } from 'rxjs';

@Component({
    selector: 'thin-layout',
    templateUrl: './thin.component.html',
    animations: [
        fuseAnimations,
        trigger('smoothToggle', [
            transition(':enter', [
                style({
                    opacity: 0,
                    transform: 'translateX(-30px) scale(0.98)',
                }),
                animate(
                    '350ms cubic-bezier(0.4, 0, 0.2, 1)',
                    style({
                        opacity: 1,
                        transform: 'translateX(0) scale(1)',
                    })
                ),
            ]),
            transition(':leave', [
                animate(
                    '280ms cubic-bezier(0.4, 0, 0.2, 1)',
                    style({
                        opacity: 0,
                        transform: 'translateX(-30px) scale(0.98)',
                    })
                ),
            ]),
        ]),
    ],
    encapsulation: ViewEncapsulation.None,
    imports: [
        FuseLoadingBarComponent,
        FuseVerticalNavigationComponent,
        MatButtonModule,
        MatIconModule,
        MatTooltipModule,
        FuseFullscreenComponent,
        RouterOutlet,
    ],
})
export class ThinLayoutComponent implements OnInit, OnDestroy {
    isScreenSmall: boolean;
    navigation: Navigation;
    scheme: Scheme = 'light';
    showNormalSidebar: boolean = true; // <-- ADD THIS
    private _unsubscribeAll: Subject<any> = new Subject<any>();

    /**
     * Constructor
     */
    constructor(
        private _activatedRoute: ActivatedRoute,
        private _router: Router,
        private _navigationService: NavigationService,
        private _fuseConfigService: FuseConfigService,
        private _fuseMediaWatcherService: FuseMediaWatcherService,
        private _fuseNavigationService: FuseNavigationService
    ) { }

    // -----------------------------------------------------------------------------------------------------
    // @ Accessors
    // -----------------------------------------------------------------------------------------------------

    /**
     * Getter for current year
     */
    get currentYear(): number {
        return new Date().getFullYear();
    }

    // -----------------------------------------------------------------------------------------------------
    // @ Lifecycle hooks
    // -----------------------------------------------------------------------------------------------------

    /**
     * On init
     */
    ngOnInit(): void {
        // Subscribe to navigation data
        this._navigationService.navigation$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((navigation: Navigation) => {
                this.navigation = navigation;
            });

        this._fuseConfigService.config$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((config) => {
                this.scheme = config.scheme;
            });

        // Subscribe to media changes
        this._fuseMediaWatcherService.onMediaChange$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe(({ matchingAliases }) => {
                // Check if the screen is small
                this.isScreenSmall = !matchingAliases.includes('md');
            });
    }

    /**
     * On destroy
     */
    ngOnDestroy(): void {
        // Unsubscribe from all subscriptions
        this._unsubscribeAll.next(null);
        this._unsubscribeAll.complete();
    }

    // -----------------------------------------------------------------------------------------------------
    // @ Public methods
    // -----------------------------------------------------------------------------------------------------

    /**
     * Toggle navigation (for mobile - toggle mini sidebar)
     *
     * @param name
     */
    toggleNavigation(name: string): void {
        const navigation =
            this._fuseNavigationService.getComponent<FuseVerticalNavigationComponent>(
                name
            );

        if (navigation) {
            navigation.toggle();
        }
    }

    /**
     * Toggle normal sidebar (when clicking hamburger in mini sidebar)
     */
    toggleNormalSidebar(): void {
        const navigation =
            this._fuseNavigationService.getComponent<FuseVerticalNavigationComponent>(
                'mainNavigation'
            );

        if (navigation) {
            navigation.toggleNormalSidebar();
            // Sync local property
            this.showNormalSidebar = !this.showNormalSidebar;
        }
    }

    isDarkMode(): boolean {
        if (this.scheme === 'dark') return true;
        if (this.scheme === 'light') return false;
        return window.matchMedia('(prefers-color-scheme: dark)').matches;
    }

    toggleColorScheme(): void {
        this._fuseConfigService.config = {
            scheme: this.isDarkMode() ? 'light' : 'dark',
        };
    }
}

