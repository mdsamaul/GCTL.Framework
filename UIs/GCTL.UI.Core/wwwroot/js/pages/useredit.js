$(document).ready(function () {
    const passwordInput = document.getElementById('Password');
    const toggleBtn = document.getElementById('toggleBtn');
    const eyeIcon = document.getElementById('eyeIcon');

    const confirmPasswordInput = document.getElementById('ConfirmPassword');
    const toggleConfirmBtn = document.getElementById('toggleConfirmBtn');
    const eyeConfirmIcon = document.getElementById('eyeConfirmIcon');

    const tooltip = document.getElementById('passwordTooltip');

    let isAnimating = false;

    function stopPasswordTooltip() {
        if (!tooltip) return;
        isAnimating = false;
        tooltip.classList.remove('show');
        tooltip.style.display = 'none';
        tooltip.style.opacity = '0';
    }

    function updateToggleVisibility() {
        if (!passwordInput || !toggleBtn) return;
        toggleBtn.style.display = passwordInput.value.length > 0 ? 'block' : 'none';
    }

    function updateConfirmToggleVisibility() {
        if (!confirmPasswordInput || !toggleConfirmBtn) return;
        toggleConfirmBtn.style.display = confirmPasswordInput.value.length > 0 ? 'block' : 'none';
    }

    function toggleInputPassword(input, icon) {
        if (!input || !icon) return;
        const cursorPos = input.selectionStart;
        if (input.type === 'password') {
            input.type = 'text';
            icon.classList.remove('fa-eye');
            icon.classList.add('fa-eye-slash');
        } else {
            input.type = 'password';
            icon.classList.remove('fa-eye-slash');
            icon.classList.add('fa-eye');
        }
        if (cursorPos !== null) {
            input.setSelectionRange(cursorPos, cursorPos);
            input.focus();
        }
    }

    // Password input events
    if (passwordInput) {
        passwordInput.addEventListener('input', function () {
            updateToggleVisibility();
            // Turn off blinking tooltip immediately upon any typing/change
            stopPasswordTooltip();
        });
        passwordInput.addEventListener('focus', function () {
            updateToggleVisibility();
        });
        passwordInput.addEventListener('blur', function () {
            if (toggleBtn) toggleBtn.style.display = 'none';
        });
    }

    if (toggleBtn) {
        toggleBtn.addEventListener('click', function () {
            toggleInputPassword(passwordInput, eyeIcon);
        });
        toggleBtn.addEventListener('mousedown', function (e) { e.preventDefault(); });
    }

    // Confirm Password input events
    if (confirmPasswordInput) {
        confirmPasswordInput.addEventListener('input', updateConfirmToggleVisibility);
        confirmPasswordInput.addEventListener('focus', updateConfirmToggleVisibility);
        confirmPasswordInput.addEventListener('blur', function () {
            if (toggleConfirmBtn) toggleConfirmBtn.style.display = 'none';
        });
    }

    if (toggleConfirmBtn) {
        toggleConfirmBtn.addEventListener('click', function () {
            toggleInputPassword(confirmPasswordInput, eyeConfirmIcon);
        });
        toggleConfirmBtn.addEventListener('mousedown', function (e) { e.preventDefault(); });
    }

    // Animated Tooltip Prompt
    function showPasswordTooltip(text) {
        if (!tooltip) return;
        tooltip.textContent = text;
        tooltip.classList.add('show');
        tooltip.style.display = 'block';

        if (!isAnimating) {
            isAnimating = true;
            animateLoop();
        }
    }

    function animateLoop() {
        if (!tooltip || !isAnimating) return;
        tooltip.style.opacity = '0';
        tooltip.style.transform = 'translateY(-50%) scale(0.8)';

        let start = null;
        const duration = 2000;

        function animate(timestamp) {
            if (!isAnimating) return;
            if (!start) start = timestamp;
            const progress = (timestamp - start) / duration;

            if (progress < 0.5) {
                const t = progress * 2;
                const scale = 0.8 + (0.25 * easeOutBack(t));
                tooltip.style.transform = `translateY(-50%) scale(${scale})`;
                tooltip.style.opacity = t;
            } else {
                const t = (progress - 0.5) * 2;
                const scale = 1.05 - (0.05 * t);
                tooltip.style.transform = `translateY(-50%) scale(${scale})`;
                tooltip.style.opacity = 1;
            }

            if (progress < 1) {
                requestAnimationFrame(animate);
            } else {
                tooltip.style.transform = 'translateY(-50%) scale(1)';
                tooltip.style.opacity = '1';

                setTimeout(function () {
                    if (isAnimating && tooltip && tooltip.classList.contains('show')) {
                        animateLoop();
                    } else {
                        isAnimating = false;
                    }
                }, 400);
            }
        }

        function easeOutBack(t) {
            const c1 = 1.70158;
            const c3 = c1 + 1;
            return 1 + c3 * Math.pow(t - 1, 3) + c1 * Math.pow(t - 1, 2);
        }

        requestAnimationFrame(animate);
    }

    // Fetch and Populate Form
    function populateForm(id) {
        const url = (id !== undefined && id !== null && id !== '') ? `/EditUser/GetById/${id}` : '/EditUser/GetById';

        $.ajax({
            url: url,
            type: 'GET',
            success: function (res) {
                if (res && res.data) {
                    const data = res.data;
                    $('#UserId').val(data.userId || 0);
                    $('#Username').val(data.username || '');
                    $('#FirstName').val(data.firstName || '');
                    $('#LastName').val(data.lastName || '');
                    $('#EmployeeId').val(data.employeeId || '');
                    $('#Dob').val(data.dob || '');
                    $('#OffPhone').val(data.offPhone || '');
                    $('#PerPhone').val(data.perPhone || '');
                    $('#OffEmail').val(data.offEmail || '');
                    $('#PerEmail').val(data.perEmail || '');
                    $('#WorkStation').val(data.workStation || '');
                    $('#Regulation').val(data.regulation || '');
                    $('#Password').val('');
                    $('#ConfirmPassword').val('');
                    $('#SecureCode').val(data.secureCode || '');
                }
            },
            error: function () {
                if (typeof toastr !== 'undefined') {
                    toastr.error('Failed to load user profile data.');
                }
            }
        });
    }

    // Initialize Form and Tooltip
    populateForm();
    showPasswordTooltip("Change Your Password");

    // Clear Button Handler
    $(document).on('click', '.js-edituser-clear', function () {
        $('#Password').val('');
        $('#ConfirmPassword').val('');
        $('#SecureCode').val('');
        updateToggleVisibility();
        updateConfirmToggleVisibility();
        showPasswordTooltip("Change Your Password");
    });

    // Save Button Handler
    $(document).on('click', '.js-edituser-save', function () {
        const password = $('#Password').val();
        const confirmPassword = $('#ConfirmPassword').val();

        if (!password || password.trim().length === 0) {
            if (typeof toastr !== 'undefined') {
                toastr.warning('Please enter a new password.');
            } else {
                alert('Please enter a new password.');
            }
            $('#Password').focus();
            return;
        }

        if (password !== confirmPassword) {
            if (typeof toastr !== 'undefined') {
                toastr.warning('New Password and Confirm Password do not match.');
            } else {
                alert('New Password and Confirm Password do not match.');
            }
            $('#ConfirmPassword').focus();
            return;
        }

        const formData = new FormData($('#editUserForm')[0]);

        $.ajax({
            url: '/EditUser/Save',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                if (response.isSuccess) {
                    if (typeof toastr !== 'undefined') {
                        toastr.success(response.message || 'Password updated successfully.');
                    } else {
                        alert(response.message || 'Password updated successfully.');
                    }
                    $('#Password').val('');
                    $('#ConfirmPassword').val('');
                    updateToggleVisibility();
                    updateConfirmToggleVisibility();

                    // Reload grid if DataTable is initialized
                    if ($.fn.DataTable && $.fn.DataTable.isDataTable('#editUserGrid')) {
                        $('#editUserGrid').DataTable().ajax.reload(null, false);
                    }
                } else {
                    if (typeof toastr !== 'undefined') {
                        toastr.error(response.message || 'Failed to update password.');
                    } else {
                        alert(response.message || 'Failed to update password.');
                    }
                }
            },
            error: function () {
                if (typeof toastr !== 'undefined') {
                    toastr.error('An unexpected server error occurred.');
                } else {
                    alert('An unexpected server error occurred.');
                }
            }
        });
    });
});
