import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { NgZone } from '@angular/core';
import { ToastService, ToastType } from './toast.service';

/**
 * ToastService Test Suite
 * 
 * Covers:
 * 1. Signal integrity (immediate state updates)
 * 2. Auto-dismissal behavior with fakeAsync
 * 3. Zone behavior (runOutsideAngular verification)
 * 4. Manual dismiss functionality
 * 5. Successive toast handling
 */
describe('ToastService', () => {
    let service: ToastService;
    let ngZone: NgZone;

    beforeEach(() => {
        TestBed.configureTestingModule({});
        service = TestBed.inject(ToastService);
        ngZone = TestBed.inject(NgZone);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    describe('Signal Integrity', () => {
        it('should update current() signal immediately when show() is called', () => {
            expect(service.current()).toBeNull();

            service.show('Test message', 'success');

            const toast = service.current();
            expect(toast).not.toBeNull();
            expect(toast?.msg).toBe('Test message');
            expect(toast?.type).toBe('success');
        });

        it('should support both success and error types', () => {
            service.show('Success message', 'success');
            expect(service.current()?.type).toBe('success');

            service.show('Error message', 'error');
            expect(service.current()?.type).toBe('error');
        });

        it('should default to success type when not specified', () => {
            service.show('Default message');
            expect(service.current()?.type).toBe('success');
        });
    });

    describe('Auto-Dismissal', () => {
        it('should auto-dismiss after 3000ms', fakeAsync(() => {
            service.show('Auto dismiss test', 'success');
            expect(service.current()).not.toBeNull();

            // Advance time by 2999ms (just before dismissal)
            tick(2999);
            expect(service.current()).not.toBeNull();

            // Advance the final 1ms to trigger dismissal
            tick(1);
            expect(service.current()).toBeNull();
        }));

        it('should cancel previous timer when showing new toast', fakeAsync(() => {
            service.show('First message', 'success');
            tick(1500);

            // Show new toast before first one dismisses
            service.show('Second message', 'error');
            expect(service.current()?.msg).toBe('Second message');

            // Advance 1500ms more (total 3000ms from first toast)
            // First timer should be cancelled, so toast should still be visible
            tick(1500);
            expect(service.current()).not.toBeNull();
            expect(service.current()?.msg).toBe('Second message');

            // Advance to complete second toast's timer (3000ms from second show)
            tick(1500);
            expect(service.current()).toBeNull();
        }));
    });

    describe('Zone Behavior', () => {
        it('should run dismissal timer outside Angular zone', fakeAsync(() => {
            const runOutsideAngularSpy = spyOn(ngZone, 'runOutsideAngular').and.callThrough();
            const runSpy = spyOn(ngZone, 'run').and.callThrough();

            service.show('Zone test', 'success');

            // Verify runOutsideAngular was called for setTimeout
            expect(runOutsideAngularSpy).toHaveBeenCalled();

            // Complete the timer
            tick(3000);

            // Verify zone.run() was called when clearing the state
            expect(runSpy).toHaveBeenCalled();
        }));
    });

    describe('Manual Dismiss', () => {
        it('should clear state immediately when dismiss() is called', () => {
            service.show('Dismiss test', 'success');
            expect(service.current()).not.toBeNull();

            service.dismiss();

            expect(service.current()).toBeNull();
        });

        it('should cancel pending auto-dismiss timer', fakeAsync(() => {
            service.show('Timer cancel test', 'success');
            tick(1500);

            service.dismiss();
            expect(service.current()).toBeNull();

            // Advance past original auto-dismiss time
            tick(2000);
            // Should still be null (timer was cancelled)
            expect(service.current()).toBeNull();
        }));

        it('should handle dismiss() when no toast is showing', () => {
            expect(service.current()).toBeNull();

            expect(() => service.dismiss()).not.toThrow();

            expect(service.current()).toBeNull();
        });
    });

    describe('Edge Cases', () => {
        it('should handle rapid successive calls', fakeAsync(() => {
            service.show('Message 1', 'success');
            service.show('Message 2', 'error');
            service.show('Message 3', 'success');

            expect(service.current()?.msg).toBe('Message 3');

            tick(3000);
            expect(service.current()).toBeNull();
        }));

        it('should handle empty message', () => {
            service.show('', 'success');
            expect(service.current()?.msg).toBe('');
        });

        it('should preserve message integrity with special characters', () => {
            const specialMsg = 'Test <div>HTML</div> & "quotes" \'apostrophes\'';
            service.show(specialMsg, 'error');
            expect(service.current()?.msg).toBe(specialMsg);
        });
    });
});
