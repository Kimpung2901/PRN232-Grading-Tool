import { Injectable } from '@angular/core';
import { Navigation } from 'app/core/navigation/navigation.types';
import { compactNavigation, defaultNavigation, futuristicNavigation, horizontalNavigation } from 'app/core/navigation/navigation.data';
import { Observable, of, ReplaySubject, tap } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class NavigationService {
    private _navigation: ReplaySubject<Navigation> =
        new ReplaySubject<Navigation>(1);

    // -----------------------------------------------------------------------------------------------------
    // @ Accessors
    // -----------------------------------------------------------------------------------------------------

    /**
     * Getter for navigation
     */
    get navigation$(): Observable<Navigation> {
        return this._navigation.asObservable();
    }

    // -----------------------------------------------------------------------------------------------------
    // @ Public methods
    // -----------------------------------------------------------------------------------------------------

    /**
     * Get all navigation data
     */
    get(): Observable<Navigation> {
        const navigation: Navigation = {
            compact: compactNavigation,
            default: defaultNavigation,
            futuristic: futuristicNavigation,
            horizontal: horizontalNavigation
        };

        return of(navigation).pipe(
            tap((nav) => {
                this._navigation.next(nav);
            })
        );
    }
}
