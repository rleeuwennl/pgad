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

// Load external HTML fragment for Webteam and inject into #content
(function($){
	function swapContent(html){
		var $c = $('#content');
		if (!$c.length) return;
		$c.fadeOut(120, function(){
			$c.html(html).fadeIn(150);
		});
	}

	$(function(){
		$('#nav a').filter(function(){ return $(this).text().trim().toLowerCase() === 'webteam'; })
			.on('click', function(e){
				e.preventDefault();
				// Load the fragment and inject
				$.get('webteam.html').done(function(data){
					swapContent(data);
				}).fail(function(){
					swapContent('<p>Kon webteam-pagina niet laden.</p>');
				});

				if ($('#navPanel').length) { $('#navPanel').removeClass('navPanel-visible'); }
			});
	});
})(jQuery);