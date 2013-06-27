#!/usr/bin/perl
#fetch Hg Gravatars

use strict;
use warnings;

use LWP::Simple;
use Digest::MD5 qw(md5_hex);

my $size       = 90;
my $output_dir = '.hg/avatar';

die("no .hg/ directory found in current path\n") unless -d '.hg';

mkdir($output_dir) unless -d $output_dir;

open(HGLOG, q/hg log --template "{author|email}|{author|person}\n" |/) or die("failed to read hg-log: $!\n");

my %processed_authors;

while(<HGLOG>) {
    chomp;
    my($email, $author) = split(/\|/, $_);

    next if $processed_authors{$author}++;

    my $author_image_file = $output_dir . '/' . $author . '.png';

    #skip images we have
    next if -e $author_image_file;

    #try and fetch image

    my $grav_url = "http://www.gravatar.com/avatar/".md5_hex(lc $email)."?d=404&size=".$size; 

    warn "fetching image for '$author' $email ($grav_url)...\n";

    my $rc = getstore($grav_url, $author_image_file);

    sleep(1);

    if($rc != 200) {
        unlink($author_image_file);
        next;
    }
}

close HGLOG;