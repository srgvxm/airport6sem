document.addEventListener('DOMContentLoaded', function() {
    const authCard = document.querySelector('.auth-card');
    if (authCard) {
        setTimeout(() => {
            authCard.classList.add('visible');
        }, 100);
    }
    const inputs = document.querySelectorAll('.auth-form-group input');
    inputs.forEach(input => {
        if (input.value.trim() !== '') {
            input.classList.add('has-value');
        }
        input.addEventListener('focus', function() {
            this.parentElement.classList.add('focused');
        });
        input.addEventListener('blur', function() {
            this.parentElement.classList.remove('focused');
            if (this.value.trim() !== '') {
                this.classList.add('has-value');
            } else {
                this.classList.remove('has-value');
            }
        });
        input.addEventListener('input', function() {
            if (this.value.trim() !== '') {
                this.classList.add('has-value');
            } else {
                this.classList.remove('has-value');
            }
        });
    });
}); 

