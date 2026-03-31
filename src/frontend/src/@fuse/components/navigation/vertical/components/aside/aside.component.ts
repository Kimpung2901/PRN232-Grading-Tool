import { BooleanInput } from '@angular/cdk/coercion';
import { NgClass } from '@angular/common';
import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    Input,
    OnChanges,
    OnDestroy,
    OnInit,
    SimpleChanges,
    inject,
} from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { NavigationEnd, Router } from '@angular/router';
import { FuseNavigationService } from '@fuse/components/navigation/navigation.service';
import { FuseNavigationItem } from '@fuse/components/navigation/navigation.types';
import { FuseVerticalNavigationBasicItemComponent } from '@fuse/components/navigation/vertical/components/basic/basic.component';
import { FuseVerticalNavigationCollapsableItemComponent } from '@fuse/components/navigation/vertical/components/collapsable/collapsable.component';
import { FuseVerticalNavigationDividerItemComponent } from '@fuse/components/navigation/vertical/components/divider/divider.component';
import { FuseVerticalNavigationGroupItemComponent } from '@fuse/components/navigation/vertical/components/group/group.component';
import { FuseVerticalNavigationSpacerItemComponent } from '@fuse/components/navigation/vertical/components/spacer/spacer.component';
import { FuseVerticalNavigationComponent } from '@fuse/components/navigation/vertical/vertical.component';
import { Subject, filter, takeUntil } from 'rxjs';

@Component({
    selector: 'fuse-vertical-navigation-aside-item',
    templateUrl: './aside.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        NgClass,
        MatTooltipModule,
        MatIconModule,
        FuseVerticalNavigationBasicItemComponent,
        FuseVerticalNavigationCollapsableItemComponent,
        FuseVerticalNavigationDividerItemComponent,
        FuseVerticalNavigationGroupItemComponent,
        FuseVerticalNavigationSpacerItemComponent,
    ],
})
export class FuseVerticalNavigationAsideItemComponent
    implements OnChanges, OnInit, OnDestroy
{
    /* eslint-disable @typescript-eslint/naming-convention */
    static ngAcceptInputType_autoCollapse: BooleanInput;
    static ngAcceptInputType_skipChildren: BooleanInput;
    /* eslint-enable @typescript-eslint/naming-convention */

    private _changeDetectorRef = inject(ChangeDetectorRef);
    private _router = inject(Router);
    private _fuseNavigationService = inject(FuseNavigationService);

    @Input() activeItemId: string;
    @Input() autoCollapse: boolean;
    @Input() item: FuseNavigationItem;
    @Input() name: string;
    @Input() skipChildren: boolean;

    active: boolean = false;
    private _fuseVerticalNavigationComponent: FuseVerticalNavigationComponent;
    private _unsubscribeAll: Subject<any> = new Subject<any>();

    ngOnChanges(changes: SimpleChanges): void {
        if ('activeItemId' in changes) {
            this._markIfActive(this._router.url);
        }
    }

    ngOnInit(): void {
        this._markIfActive(this._router.url);

        this._router.events
            .pipe(
                filter(
                    (event): event is NavigationEnd =>
                        event instanceof NavigationEnd
                ),
                takeUntil(this._unsubscribeAll)
            )
            .subscribe((event: NavigationEnd) => {
                this._markIfActive(event.urlAfterRedirects);
            });

        this._fuseVerticalNavigationComponent =
            this._fuseNavigationService.getComponent(this.name);

        this._fuseVerticalNavigationComponent.onRefreshed
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe(() => {
                this._changeDetectorRef.markForCheck();
            });
    }

    ngOnDestroy(): void {
        this._unsubscribeAll.next(null);
        this._unsubscribeAll.complete();
    }

    trackByFn(index: number, item: any): any {
        return item.id || index;
    }

    private _hasActiveChild(
        item: FuseNavigationItem,
        currentUrl: string
    ): boolean {
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

            if (child.type !== 'basic') {
                continue;
            }

            if (
                child.link &&
                this._router.isActive(child.link, child.exactMatch || false)
            ) {
                return true;
            }
        }

        return false;
    }

    private _markIfActive(currentUrl: string): void {
        // For MINI SIDEBAR (skipChildren: true)
        if (this.skipChildren) {
            this.active = this.activeItemId === this.item.id;
            this._changeDetectorRef.detectChanges();
            return;
        }

        // For NORMAL SIDEBAR (skipChildren: false)
        this.active = this.activeItemId === this.item.id;

        if (!this.active && this._hasActiveChild(this.item, currentUrl)) {
            this.active = true;
        }

        this._changeDetectorRef.markForCheck();
    }
}
