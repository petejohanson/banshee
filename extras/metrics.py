#!/usr/bin/env python

import cgi
import os

# INITIAL SETUP:
# 1. mkdir data
# 2. chmod o-xr data
# 3. echo 0 > data/count
# 4. change data_dir below
data_dir = '/home/bansheeweb/download.banshee-project.org/metrics/data/';

uploaded = False
form = cgi.FieldStorage()

if form.file:
    # Read the current count
    f = open(data_dir + 'count', 'r')
    count = f.read ()
    count = int(count)
    f.close ()

    # Increment it and write it out
    f = open(data_dir + 'count', 'w')
    count = count + 1
    f.write (str(count));
    f.close ();

    # Save the POSTed file
    filename = data_dir + str(count) + '.json'
    f = open(filename, 'w')

    while 1:
        line = form.file.readline()
        if not line: break
        f.write (line)
    
    f.close ();

    # gzip it
    os.system ('gzip ' + filename)
    uploaded = True

if uploaded:
    print "Status-Code: 200"
    print "Content-type: text/html"
    print
else:
    print "Status-Code: 500"
