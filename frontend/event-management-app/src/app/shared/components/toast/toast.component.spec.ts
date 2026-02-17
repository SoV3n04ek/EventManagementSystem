import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ToastComponent } from './toast.component';
import { ToastService } from '../../services/toast.service';
import { signal } from '@angular/core';

/**
 * ToastComponent Test Suite
 * 
 * Covers:
 * 1. UI rendering based on toast state
 * 2. CSS class application based on type
 * 3. Dismiss button functionality
 * 4. Icon rendering for success/error
 * 5. Conditional rendering with @if
 */
describe('ToastComponent', () => {
    let component: ToastComponent;
    let fixture: ComponentFixture<ToastComponent>;
    let toastService: ToastService;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [ToastComponent]
        }).compileComponents();

        fixture = TestBed.createComponent(ToastComponent);
        component = fixture.componentInstance;
        toastService = TestBed.inject(ToastService);
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    describe('UI Rendering', () => {
        it('should not render when no toast is active', () => {
            const compiled = fixture.nativeElement as HTMLElement;
            expect(compiled.querySelector('.toast-container')).toBeNull();
        });

        it('should render toast when service shows a message', () => {
            toastService.show('Test notification', 'success');
            fixture.detectChanges();

            const compiled = fixture.nativeElement as HTMLElement;
            const container = compiled.querySelector('.toast-container');
            expect(container).not.toBeNull();

            const message = compiled.querySelector('.toast-message');
            expect(message?.textContent?.trim()).toBe('Test notification');
        });

        it('should update when toast changes', () => {
            toastService.show('First message', 'success');
            fixture.detectChanges();

            let message = fixture.nativeElement.querySelector('.toast-message');
            expect(message?.textContent?.trim()).toBe('First message');

            toastService.show('Second message', 'error');
            fixture.detectChanges();

            message = fixture.nativeElement.querySelector('.toast-message');
            expect(message?.textContent?.trim()).toBe('Second message');
        });

        it('should remove toast from DOM when dismissed', () => {
            toastService.show('Dismissible toast', 'success');
            fixture.detectChanges();

            expect(fixture.nativeElement.querySelector('.toast-container')).not.toBeNull();

            toastService.dismiss();
            fixture.detectChanges();

            expect(fixture.nativeElement.querySelector('.toast-container')).toBeNull();
        });
    });

    describe('CSS Classes', () => {
        it('should apply toast-success class for success type', () => {
            toastService.show('Success message', 'success');
            fixture.detectChanges();

            const toast = fixture.nativeElement.querySelector('.toast');
            expect(toast?.classList.contains('toast-success')).toBe(true);
            expect(toast?.classList.contains('toast-error')).toBe(false);
        });

        it('should apply toast-error class for error type', () => {
            toastService.show('Error message', 'error');
            fixture.detectChanges();

            const toast = fixture.nativeElement.querySelector('.toast');
            expect(toast?.classList.contains('toast-error')).toBe(true);
            expect(toast?.classList.contains('toast-success')).toBe(false);
        });

        it('should have consistent base classes regardless of type', () => {
            toastService.show('Test', 'success');
            fixture.detectChanges();

            const container = fixture.nativeElement.querySelector('.toast-container');
            const toast = fixture.nativeElement.querySelector('.toast');

            expect(container).not.toBeNull();
            expect(toast).not.toBeNull();
        });
    });

    describe('Icon Rendering', () => {
        it('should render success icon (checkmark) for success type', () => {
            toastService.show('Success', 'success');
            fixture.detectChanges();

            const icons = fixture.nativeElement.querySelectorAll('.toast-icon');
            expect(icons.length).toBeGreaterThan(0);

            // Success icon has path with checkmark (M5 13l4 4L19 7)
            const paths = fixture.nativeElement.querySelectorAll('.toast-content svg path');
            const hasCheckmark = Array.from(paths).some((path: any) =>
                path.getAttribute('d')?.includes('M5 13l4 4L19 7')
            );
            expect(hasCheckmark).toBe(true);
        });

        it('should render error icon (X) for error type', () => {
            toastService.show('Error', 'error');
            fixture.detectChanges();

            const icons = fixture.nativeElement.querySelectorAll('.toast-icon');
            expect(icons.length).toBeGreaterThan(0);

            // Error icon has path with X pattern (M6 18L18 6M6 6l12 12)
            const paths = fixture.nativeElement.querySelectorAll('.toast-content svg path');
            const hasX = Array.from(paths).some((path: any) =>
                path.getAttribute('d')?.includes('M6 18L18 6M6 6l12 12')
            );
            expect(hasX).toBe(true);
        });
    });

    describe('Dismiss Functionality', () => {
        it('should have a dismiss button when toast is showing', () => {
            toastService.show('Test', 'success');
            fixture.detectChanges();

            const dismissBtn = fixture.nativeElement.querySelector('.toast-dismiss');
            expect(dismissBtn).not.toBeNull();
        });

        it('should call toastService.dismiss() when dismiss button is clicked', () => {
            spyOn(toastService, 'dismiss');

            toastService.show('Test', 'success');
            fixture.detectChanges();

            const dismissBtn = fixture.nativeElement.querySelector('.toast-dismiss') as HTMLButtonElement;
            dismissBtn.click();

            expect(toastService.dismiss).toHaveBeenCalled();
        });

        it('should remove toast from view after dismiss button click', () => {
            toastService.show('Test', 'success');
            fixture.detectChanges();

            const dismissBtn = fixture.nativeElement.querySelector('.toast-dismiss') as HTMLButtonElement;
            dismissBtn.click();
            fixture.detectChanges();

            expect(fixture.nativeElement.querySelector('.toast-container')).toBeNull();
        });
    });

    describe('Accessibility', () => {
        it('should have role="alert" on toast element', () => {
            toastService.show('Accessible toast', 'success');
            fixture.detectChanges();

            const toast = fixture.nativeElement.querySelector('.toast');
            expect(toast?.getAttribute('role')).toBe('alert');
        });

        it('should have aria-label on dismiss button', () => {
            toastService.show('Test', 'success');
            fixture.detectChanges();

            const dismissBtn = fixture.nativeElement.querySelector('.toast-dismiss');
            expect(dismissBtn?.getAttribute('aria-label')).toBe('Dismiss notification');
        });
    });

    describe('OnPush Change Detection', () => {
        it('should update when signal changes with OnPush strategy', () => {
            // This verifies that the component respects OnPush and only updates via signal changes
            toastService.show('Initial', 'success');
            fixture.detectChanges();

            expect(fixture.nativeElement.textContent).toContain('Initial');

            toastService.show('Updated', 'error');
            fixture.detectChanges();

            expect(fixture.nativeElement.textContent).toContain('Updated');
            expect(fixture.nativeElement.textContent).not.toContain('Initial');
        });
    });
});
