// Simple authorization module
(function($) {
    'use strict';

    var SimpleAuth = {
        token: null,
        isAuthenticated: false,

        init: function() {
            this.token = localStorage.getItem('pgad_token');
            if (this.token) {
                this.isAuthenticated = true;
                this.showAuthUI();
            }

            // Create login modal HTML
            this.createLoginModal();

            // Listen for Ctrl+Shift+L
            $(document).on('keydown', function(e) {
                if (e.ctrlKey && e.shiftKey && e.keyCode === 76) {
                    e.preventDefault();
                    if (!SimpleAuth.isAuthenticated) {
                        SimpleAuth.showLoginModal();
                    }
                }
            });
        },

        createLoginModal: function() {
            var modalHtml = `
                <div id="pgad-login-modal" class="pgad-login-modal">
                    <div class="pgad-login-container">
                        <div class="pgad-login-header">
                            <h2>Admin Login</h2>
                            <p>Protestantse Gemeente Angerlo-Doesburg</p>
                        </div>
                        <form id="pgad-login-form">
                            <div class="pgad-form-group">
                                <label for="pgad-username">Username</label>
                                <input type="text" id="pgad-username" name="username" placeholder="Enter username" required autocomplete="username" />
                            </div>
                            <div class="pgad-form-group">
                                <label for="pgad-password">Password</label>
                                <input type="password" id="pgad-password" name="password" placeholder="Enter password" required autocomplete="current-password" />
                            </div>
                            <div id="pgad-login-error" class="pgad-login-error"></div>
                            <div class="pgad-form-actions">
                                <button type="submit" class="pgad-btn-login">Login</button>
                                <button type="button" class="pgad-btn-cancel" onclick="SimpleAuth.closeLoginModal()">Cancel</button>
                            </div>
                        </form>
                    </div>
                </div>
            `;
            $('body').append(modalHtml);
            
            $('#pgad-login-form').on('submit', function(e) {
                e.preventDefault();
                SimpleAuth.login($('#pgad-username').val(), $('#pgad-password').val());
            });
        },

        showLoginModal: function() {
            $('#pgad-login-modal').addClass('pgad-active');
            $('#pgad-username').focus();
        },

        closeLoginModal: function() {
            $('#pgad-login-modal').removeClass('pgad-active');
            $('#pgad-login-form')[0].reset();
            $('#pgad-login-error').text('').hide();
        },

        login: function(username, password) {
            $.ajax({
                url: '/api/auth/login',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ username: username, password: password })
            })
            .done(function(data) {
                if (data.success && data.token) {
                    SimpleAuth.token = data.token;
                    SimpleAuth.isAuthenticated = true;
                    localStorage.setItem('pgad_token', SimpleAuth.token);
                    SimpleAuth.closeLoginModal();
                    SimpleAuth.showAuthUI();
                    console.log('Authorized token: ' + SimpleAuth.token);
                } else {
                    $('#pgad-login-error').text('Login failed').show();
                }
            })
            .fail(function() {
                $('#pgad-login-error').text('Invalid username or password').show();
            });
        },

        logout: function() {
            $.ajax({
                url: '/api/auth/logout',
                method: 'POST',
                headers: {
                    'X-Auth-Token': this.token
                }
            })
            .always(function() {
                SimpleAuth.token = null;
                SimpleAuth.isAuthenticated = false;
                localStorage.removeItem('pgad_token');
                SimpleAuth.hideAuthUI();
            });
        },

        showAuthUI: function() {
            $('body').append(
                '<div id="pgad-auth-badge" class="pgad-auth-badge">' +
                '<span class="pgad-badge-dot"></span> Authorized' +
                '<button id="pgad-logout-btn" class="pgad-logout-btn">Logout</button>' +
                '</div>'
            );
            $('#pgad-logout-btn').on('click', function() {
                SimpleAuth.logout();
            });
        },

        hideAuthUI: function() {
            $('#pgad-auth-badge').fadeOut(300, function() { $(this).remove(); });
        },

        // Get header for authorized requests
        getAuthHeader: function() {
            return this.token ? { 'X-Auth-Token': this.token } : {};
        }
    };

    $(document).ready(function() {
        SimpleAuth.init();
    });

    window.SimpleAuth = SimpleAuth;

})(jQuery);
