document.querySelectorAll('[data-schedule-confirm="true"]').forEach(button => {
    let reviewing = false;
    button.addEventListener('click', async event => {
        event.preventDefault();
        if (reviewing) return;
        reviewing = true;
        const escape = text => $('<div>').text(text).html();
        try {
            const response = await fetch(button.href, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            if (!response.ok) throw new Error('Unable to review this schedule. Refresh and check your access.');
            const html = await response.text();
            const document = new DOMParser().parseFromString(html, 'text/html');
            const form = document.getElementById('confirmSchedule');
            if (!form) throw new Error('This schedule is no longer awaiting confirmation. Refresh its details.');
            const body = new URLSearchParams(new FormData(form));
            const review = await ModernAlert.confirm({ title: 'Review and Confirm', text: html, icon: 'question', confirmText: 'Confirm & Create Job Order', cancelText: 'Keep reviewing' });
            if (!review.isConfirmed) return;
            const submit = async () => {
                ModernAlert.showLoading('Confirming schedule…');
                const saved = await fetch(form.getAttribute('action'), { method: 'POST', body, headers: { 'X-Requested-With': 'XMLHttpRequest' } });
                if (!saved.ok) throw new Error('Unable to confirm this schedule. Refresh and try again.');
                return saved.json();
            };
            let result = await submit();
            if (result.requiresConflictAcknowledgement) {
                const conflicts = await ModernAlert.confirm({ title: 'Review schedule conflicts', text: escape(result.message), icon: 'warning', confirmText: 'Continue to final confirmation', cancelText: 'Keep reviewing' });
                if (!conflicts.isConfirmed) return;
                const acknowledgement = await ModernAlert.confirm({ title: 'Confirm this overlapping booking?', text: 'I acknowledge the overlapping bookings and want to create the Job Order.', icon: 'warning', confirmText: 'Acknowledge & Create Job Order', cancelText: 'Keep reviewing' });
                if (!acknowledgement.isConfirmed) return;
                body.set('allowConflicts', 'true');
                result = await submit();
            }
            if (result.success && result.redirectUrl) {
                window.location.href = result.redirectUrl;
            } else {
                ModernAlert.error(escape(result.message || 'Schedule confirmation failed.'));
            }
        } catch (error) {
            ModernAlert.error(escape(error.message));
        } finally {
            reviewing = false;
        }
    });
    if (new URLSearchParams(window.location.search).get('review') === 'true') button.click();
});
