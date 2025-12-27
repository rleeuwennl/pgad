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

	// Fragment loader: single handler for nav and navPanel (capture to beat dropotron)
	(function(){
		function swapContent(html){
			var $c = $('#content');
			if (!$c.length) return;
			$c.fadeOut(120, function(){
				$c.html(html).fadeIn(150);
			});
		}

		var fragmentMap = {
			'webteam': 'webteam.html',
			'pastoraalteam': 'pastoraalteam.html',
			'contact': 'contact.html',
			'kerkenraad': 'kerkenraad.html',
			'moderamen': 'moderamen.html',
			'voorgangers': 'voorgangers.html',			
			'diaconaat': 'diaconaat.html',
			'bloemengroet': 'bloemengroet.html',
			'anbidiakoniepgad': 'anbi-diaconie-pgad.html',
			'projectcolumbia': 'project-columbia.html',
			'perspective': 'perspective.html',
			'tentofnations': 'tent-of-nations.html',
			'zending': 'zending.html',
		};

		function normalizeText(text){
			return (text || '').toString().trim().toLowerCase().replace(/\s+/g, '');
		}

		console.log('Fragment loader: capture handler binding');

		document.addEventListener('click', function(e){
			var link = e.target.closest('#nav a, #navPanel a');
			if (!link) return;
			var linkText = normalizeText(link.textContent);
			var frag = fragmentMap[linkText];

			if (!frag) return;

			console.log('Fragment click detected:', linkText, '->', frag);
			e.preventDefault();
			e.stopPropagation();
			e.stopImmediatePropagation();

			$.get(frag).done(function(data){
				console.log('Fragment loaded:', frag);
				swapContent(data);
			}).fail(function(){
				console.log('Fragment load failed for', frag);
				swapContent('<p>Kon pagina niet laden.</p>');
			});

			// Close mobile panel if open
			$('body').removeClass('navPanel-visible');
		}, true); // capture phase
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