// Copyright 2010 Novell, Inc.
// Written by Aaron Bockover
// Made available under the MIT license:
// http://www.opensource.org/licenses/mit-license.php

var e = document.getElementById ('a2z_1p_flash_not_installed');
while (e) {
    if (e.tagName == 'TABLE' && e.className == 'playerRow') {
        e.style.display = 'none';
    }
    e = e.parentElement;
}

for (var l = document.getElementsByClassName ('flashWarning'), i = 0;
    l && i < l.length; i++) {
    l[i].style.display = 'none';
}
