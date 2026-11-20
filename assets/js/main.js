/*
	Escape Velocity by HTML5 UP
	html5up.net | @ajlkn
	Free for personal and commercial use under the CCA 3.0 license (html5up.net/license)
*/

(function($) {

	var	$window = $(window),
		$body = $('body');

	// Breakpoints.
		breakpoints({
			xlarge:  [ '1281px',  '1680px' ],
			large:   [ '981px',   '1280px' ],
			medium:  [ '737px',   '980px'  ],
			small:   [ null,      '736px'  ]
		});

	// Play initial animations on page load.
		$window.on('load', function() {
			window.setTimeout(function() {
				$body.removeClass('is-preload');
			}, 100);
		});

	// Mark nav parents (with submenus) as non-clickable for UX clarity
	document.addEventListener('DOMContentLoaded', function(){
		var parentLinks = document.querySelectorAll('#nav li > a');
		parentLinks.forEach(function(link){
			if (link.nextElementSibling && link.nextElementSibling.tagName === 'UL') {
				link.classList.add('nav-parent');
				link.setAttribute('href', '#');
			}
		});
	});

	// Copy email address to clipboard when a mailto link is clicked
	(function(){
		var toastTimer;

		function showToast(message){
			var toast = document.getElementById('copy-toast');
			if (!toast) {
				toast = document.createElement('div');
				toast.id = 'copy-toast';
				toast.setAttribute('role', 'status');
				toast.style.cssText = 'position:fixed;left:50%;bottom:2em;transform:translateX(-50%);' +
					'background:#333;color:#fff;padding:0.75em 1.25em;border-radius:6px;z-index:100000;' +
					'box-shadow:0 2px 8px rgba(0,0,0,0.3);transition:opacity 0.3s;';
				document.body.appendChild(toast);
			}
			toast.textContent = message;
			toast.style.opacity = '1';
			clearTimeout(toastTimer);
			toastTimer = setTimeout(function(){ toast.style.opacity = '0'; }, 2500);
		}

		function copyText(text){
			if (navigator.clipboard && window.isSecureContext) {
				return navigator.clipboard.writeText(text);
			}
			return new Promise(function(resolve, reject){
				var ta = document.createElement('textarea');
				ta.value = text;
				ta.style.cssText = 'position:fixed;opacity:0;';
				document.body.appendChild(ta);
				ta.select();
				var ok = false;
				try { ok = document.execCommand('copy'); } catch (err) {}
				document.body.removeChild(ta);
				ok ? resolve() : reject();
			});
		}

		document.addEventListener('click', function(e){
			var link = e.target.closest('a[href^="mailto:"]');
			if (!link) return;
			var address = decodeURIComponent(link.getAttribute('href').replace(/^mailto:/i, '').split('?')[0]);
			e.preventDefault();
			copyText(address).then(function(){
				showToast('E-mailadres gekopieerd: ' + address);
			}, function(){
				showToast('Kopiëren mislukt. E-mailadres: ' + address);
			});
		});
	})();

	// Fragment loader with History API: load into #content and keep URLs updated
	(function(){
		function swapContent(html){
			var $c = $('#content');
			if (!$c.length) return;
			$c.fadeOut(120, function(){
				$c.html(html).fadeIn(150);
				// Scroll to top to show beginning of new content
				window.scrollTo({
					top: 0,
					behavior: 'smooth'
				});
			});
		}

		function loadFragment(frag, opts){
			opts = opts || {};
			var requestUrl = frag.charAt(0) === '/' ? frag : '/' + frag;
			$.ajax({
				url: requestUrl,
				headers: { 'X-Fragment-Request': '1' }
			}).done(function(data){
				swapContent(data);
				if (opts.replaceHistory) {
					window.history.replaceState({ page: frag }, frag, requestUrl);
				} else if (!opts.silentHistory) {
					window.history.pushState({ page: frag }, frag, requestUrl);
				}
			}).fail(function(){
				swapContent('<p>Kon pagina niet laden.</p>');
			});
		}

		function normalizeText(text){
			return (text || '').toString().trim().toLowerCase().replace(/\s+/g, '');
		}

		function detectInitialFragment(){
			var url = new URL(window.location.href);
			var fragParam = url.searchParams.get('frag');
			if (fragParam) return fragParam;
			var path = url.pathname || '/';
			if (path !== '/' && path !== '/index.html') {
				return path.replace(/^\//, '');
			}
			return null;
		}

		console.log('Fragment loader: capture handler binding');

		document.addEventListener('click', function(e){
			var link = e.target.closest('#nav a, #navPanel a, [data-fragment]');
			if (!link) return;

			// Skip links with onclick handlers (like login)
			if (link.hasAttribute('onclick')) {
				return;
			}

			// Skip external links (http://, https://, mailto:, javascript:, etc.)
			var href = link.getAttribute('href') || '';
			if (href.match(/^(https?:|mailto:|javascript:)/i)) {
				return; // Let browser handle external links normally
			}

			// Handle home/index links with a full page reload
			if (href === 'index.html' || href === '/' || href === '') {
				e.preventDefault();
				window.location.href = '/';
				return;
			}

			// If this link opens a submenu (has a following UL), block navigation/click-through
			if (link.nextElementSibling && link.nextElementSibling.tagName === 'UL') {
				e.preventDefault();
				e.stopPropagation();
				e.stopImmediatePropagation();
				return;
			}

			var frag = link.getAttribute('data-fragment');
			if (!frag) {
				// Only load fragments for links with explicit data-fragment attribute
				// This prevents accidental loading of pages for submenu headers
				return;
			}

			e.preventDefault();
			e.stopPropagation();
			e.stopImmediatePropagation();

			loadFragment(frag);

			$('body').removeClass('navPanel-visible');
		}, true);

		window.addEventListener('popstate', function(e){
			if (e.state && e.state.page) {
				loadFragment(e.state.page, { silentHistory: true });
			} else {
				// Back to home: reload the page
				window.location.reload();
			}
		});

		// On first load, if URL points to a fragment (path or ?frag=...), load it into #content
		var initialFrag = detectInitialFragment();
		if (initialFrag) {
			loadFragment(initialFrag, { replaceHistory: true });
		}
	})();

	// Dropdowns.
		$('#nav > ul').dropotron({
			mode: 'fade',
			noOpenerFade: true,
			alignment: 'center',
			detach: false
		});

	// Nav.

		// Title Bar.
			$(
				'<div id="titleBar">' +
					'<a href="#navPanel" class="toggle"></a>' +
					'<span class="title">' + $('#logo h1').html() + '</span>' +
				'</div>'
			)
				.appendTo($body);

		// Panel.
			$(
				'<div id="navPanel">' +
					'<nav>' +
						$('#nav').navList() +
					'</nav>' +
				'</div>'
			)
				.appendTo($body)
				.panel({
					delay: 500,
					hideOnClick: true,
					hideOnSwipe: true,
					resetScroll: true,
					resetForms: true,
					side: 'left',
					target: $body,
					visibleClass: 'navPanel-visible'
				});

})(jQuery);