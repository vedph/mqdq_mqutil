# Checking Apparatus

The apparatus is exported into a copy of the original TEI documents, by clearing the content of all the `div1` elements (except for their eventual first `head` child element), and replacing it with newly generated XML code.

As explained in the [export discussion](export.md), the order of the elements in this content will vary, and in some cases (for "sparse fragments") there might not be a 1:1 correspondance.

Also, I prefer to generate comments in the exported documents, so that I can precisely refer each XML branch to its source.

So, here the best approach is probably comparing `app` with `app`, in the context of the same parent element.

## Sample

### Log

The first check is looking at the operation log and examine any eventual error (look for `[ERR]`) or warning (look for `[WRN]`).

There should be no error, while warning can be useful to ensure that nothing is unexpected. For instance, here are the first lines (omitting the timestamps for brevity):

```txt
[INF] EXPORT APPARATUS INTO TEI FILES
[INF] Groups found: 611
[INF] ABLAB-epig
[INF] Item acd5373e-d678-433d-8818-ae310dcd6870 has no fr.net.fusisoft.apparatus part
[INF] Item acd5373e-d678-433d-8818-ae310dcd6870 has no fr.net.fusisoft.apparatus:ancient part
[INF] Item acd5373e-d678-433d-8818-ae310dcd6870 has no fr.net.fusisoft.apparatus:margin part
[INF] Item acd5373e-d678-433d-8818-ae310dcd6870 has no apparatus part
[INF] ACC-frag
[WRN] Target file not exists: E:\Work\mqdqe\ACC\ACC-frag-app.xml
...
```

Here you can see that warnings are just reporting the fact that the target apparatus document is missing; this is normal for some documents, which only have text, and is not an issue.

The log reports each work as a group of items; here we have 611 groups (=works). For each group, each item is analyzed, and if it has apparatus layers they are exported. For instance, for `ABLAB-epig` there is no apparatus content, so no export happens.

### Comparing div Elements

Let us consider `VERG-eclo-app.xml`: the original document (A) is compared side by side with the updated document (B).

All what is outside `body` should be unchanged. Inside `body`, in A we first find a `div1` with some attributes, a `head` child, and a number of `app` children. All the `div1` elements in both documents should correspond 1:1.

Each `div1` should be found with the same attributes in B. Also, the `head` element if any should appear unchanged in B, as its first child element.

### List of app Elements

Let us then start with `app` elements from A: first, observe their order. In A, it is as follows:

```txt
1 from="#d001w9" to="#d001w13" type="margin-note"
2 from="#d001w9" to="#d001w9"
3 from="#d001w94" to="#d001w94"
4 from="#d001w101" to="#d001w111" type="margin-note"
5 from="#d001w101" to="#d001w101"
6 from="#d001w288" to="#d001w289"
7 from="#d001w312" to="#d001w312"
8 from="#d001w379" to="#d001w381"
9 from="#d001w402" to="#d001w402"
10 loc="d001w415 d001w420"
11 from="#d001w446" to="#d001w446"
12 from="#d001w473" to="#d001w473"
13 from="#d001w486" to="#d001w486"
14 from="#d001w488" to="#d001w488"
15 from="#d001w514" to="#d001w514"
16 from="#d001w580" to="#d001w580"
17 from="#d001w583" to="#d001w584"
18 from="#d001w586" to="#d001w586"
19 from="#d001w602" to="#d001w603"
20 from="#d001w637" to="#d001w637"
21 from="#d001w643" to="#d001w643"
22 from="#d001w645" to="#d001w645"
23 from="#d001w647" to="#d001w647"
24 from="#d001w675" to="#d001w675"
```

In B, we find another order, following the distribution of data into layers. In the following list, I prefix each entry with the corresponding entry number in A; I also include the comments. These comments are inserted on request to mark the start of a new set of data: there is one whenever a new item starts, a new part starts, or a new fragment starts. The item is recorded with its title and ID; the part with its role ID and ID; the fragment with its location. Should you need to, you can inspect items and parts by finding their ID in the database.

```txt
<!--item VERG-eclo 00001 #d001 (95736d55-7b62-4898-879e-500087e0b73a)-->
<!--apparatus fr.net.fusisoft.apparatus (3 in 333f9657-849c-49ec-9b32-3732a5250c9d)-->
<!--fr 3.1-->
=A2 from="#d001w9" to="#d001w9"
<!--fr 15.3-->
=A3 from="#d001w94" to="#d001w94"
<!--fr 16.1-->
=A5 from="#d001w101" to="#d001w101"
<!--apparatus fr.net.fusisoft.apparatus:margin (2 in 643b0135-0aec-4ff5-b4a8-4e0990a8fc52)-->

<!--fr 3.1-3.5-->
=A1 type="margin-note" from="#d001w9" to="#d001w13"
<!--fr 16.1-16.8-->
=A4 type="margin-note" from="#d001w101" to="#d001w111"

<!--item VERG-eclo 00002 #d001 (7edce359-aaec-4f35-aa19-bfb433121035)-->
<!--apparatus fr.net.fusisoft.apparatus (2 in 965032fb-fb00-4aae-a306-d2d26c172cce)-->
<!--fr 16.1-16.2-->
=A6 from="#d001w288" to="#d001w289"
<!--fr 19.7-->
=A7 from="#d001w312" to="#d001w312"

<!--item VERG-eclo 00003 #d001 (8215628c-8eaf-4bec-a13d-db90b70d03e7)-->
<!--apparatus fr.net.fusisoft.apparatus (9 in 10862db6-ac8d-429e-a785-6d3bd2644d4a)-->
<!--fr 4.5-4.6-->
=A8 from="#d001w379" to="#d001w381"
<!--fr 8.1-->
=A9 from="#d001w402" to="#d001w402"
<!--fr 10.1-->
=A10 from="#d001w415" to="#d001w415"
<!--fr 10.6-->
=A10 from="#d001w420" to="#d001w420"
<!--fr 14.3-->
=A11 from="#d001w446" to="#d001w446"
<!--fr 17.7-->
=A12 from="#d001w473" to="#d001w473"
<!--fr 20.4-->
=A13 from="#d001w486" to="#d001w486"
<!--fr 20.6-->
=A14 from="#d001w488" to="#d001w488"
<!--fr 24.4-->
=A15 from="#d001w514" to="#d001w514"

<!--item VERG-eclo 00004 #d001 (a1471fb7-d290-47fc-a0f2-54f49e2d334f)-->
<!--apparatus fr.net.fusisoft.apparatus (9 in 25a5b346-bd04-440b-810e-3b15871df989)-->
<!--fr 10.1-->
=A16 from="#d001w580" to="#d001w580"
<!--fr 10.3-10.4-->
=A17 from="#d001w583" to="#d001w584"
<!--fr 10.6-->
=A18 from="#d001w586" to="#d001w586"
<!--fr 12.3-12.4-->
=A19 from="#d001w602" to="#d001w603"
<!--fr 16.4-->
=A20 from="#d001w637" to="#d001w637"
<!--fr 18.3-->
=A21 from="#d001w643" to="#d001w643"
<!--fr 18.5-->
=A22 from="#d001w645" to="#d001w645"
<!--fr 18.7-->
=A23 from="#d001w647" to="#d001w647"
<!--fr 22.4-->
=A24 from="#d001w675" to="#d001w675"
```

Here you can see that A10, having a `loc`, was a "sparse fragment". As such, it has been duplicated into 2 entries, each located to a specific word: `d001w415` and `d001w420`.

So, all the entries found in A are present in B, nor B has any additional entry (except for that duplication).

### Original app Element 1

We can now turn to the content of each entry, comparing A with B. So, for instance, the first A entry is:

```xml
<app from="#d001w9" to="#d001w13" type="margin-note">
  <lem source="#lb1-56" type="ancient-note">
      <add type="abstract"><emph style="font-style:italic">Meditaris</emph> cantas, uel <emph style="font-style:italic">melitaris</emph>, -<emph style="font-style:italic">l</emph>- pro -<emph style="font-style:italic">d</emph>-, ut idem sit tropus.</add>
    </lem>
</app>
```

The corresponding entry in B is totally equivalent, even if not literally equal:

```xml
<!--apparatus fr.net.fusisoft.apparatus:margin (2 in 643b0135-0aec-4ff5-b4a8-4e0990a8fc52)-->
<!--fr 3.1-3.5-->
<app type="margin-note" from="#d001w9" to="#d001w13">
  <lem type="ancient-note" source="#lb1-56">
    <add type="abstract">
      <emph style="font-style:italic">Meditaris</emph> cantas, uel <emph style="font-style:italic">melitaris</emph>, -<emph style="font-style:italic">l</emph>- pro -<emph style="font-style:italic">d</emph>-, ut idem sit tropus.</add>
  </lem>
</app>
```

### Original app Element 2

Then we turn to the second entry; in A:

```xml
<app from="#d001w9" to="#d001w9">
  <lem wit="#lw1-16 #lw1-21">siluestrem</lem>
  <rdg source="#lb1-50 #lb1-25">agrestem
      <note type="details" target="#lb1-50"> 9, 4, 85,</note>
      <note type="details" target="#lb1-25"> SI 244</note>
      <ident n="d001w9">AGRESTEM</ident>
  </rdg>
  <rdg source="#lb1-56" type="ancient-note">
      <add type="abstract"><emph style="font-style:italic">silvestrem</emph>, agrestem<emph style="font-style:italic">.</emph></add>
  </rdg>
</app>
```

and in B:

```xml
<!--item VERG-eclo 00001 #d001 (95736d55-7b62-4898-879e-500087e0b73a)-->
<!--apparatus fr.net.fusisoft.apparatus (3 in 333f9657-849c-49ec-9b32-3732a5250c9d)-->
<!--fr 3.1-->
<app from="#d001w9" to="#d001w9">
  <lem wit="#lw1-16 #lw1-21">siluestrem</lem>
  <rdg source="#lb1-50 #lb1-25">agrestem<note type="details" target="#lb1-50"> 9, 4, 85,</note><note type="details" target="#lb1-25"> SI 244</note><ident n="d001w9">AGRESTEM</ident></rdg>
</app>
```

The entry is equivalent, except for the usual indentation (required by the mixed content of the parent element), and the lack of the ancient note `rdg` element. This is due to the fact that ancient notes are moved to a separate layer.

In fact, in B we find the entry after some lines, in its own `app` with the same location (`d001w9`); as we learn from comments, this is placed in a different layer part, the ancient-notes apparatus:

```xml
<!--apparatus fr.net.fusisoft.apparatus:ancient (3 in 07c4fff5-6de1-4aeb-b9b0-1678c9c516f5)-->
<!--fr 3.1-->
<app type="ancient-note" from="#d001w9" to="#d001w9">
  <rdg type="ancient-note" source="#lb1-56">
    <add type="abstract">
      <emph style="font-style:italic">silvestrem</emph>, agrestem<emph style="font-style:italic">.</emph></add>
  </rdg>
</app>
```

### Original app Element 3

In A:

```xml
<app from="#d001w94" to="#d001w94">
  <lem wit="#lw1-14" source="#lb1-50 #lb1-60 #lb1-61 #lb1-16">turbatur
      <note type="details" target="#lb1-50"> 1,4,28 (<emph style="font-style:italic">sed</emph> turbamur <emph style="font-style:italic">cod. Ambrosianus E</emph>. 153 <emph style="font-style:italic">sup</emph>.),</note>
      <note type="details" target="#lb1-60"> (<emph style="font-style:italic">sed</emph> turbamur <emph style="font-style:italic">cod. Caroliruhensis</emph> 116),</note>
      <note type="details" target="#lb1-61"> <emph style="font-style:italic">Aen</emph>. 1,272,</note>
      <note type="details" target="#lb1-16"> V p. 372,35</note>
  </lem>
  <rdg wit="#lw1-16 #lw1-21 #lw1-31 #lw1-27" source="#lb1-39">turbamur
      <ident n="d001w94">TVRBAMVR</ident>
  </rdg>
  <rdg source="#lb1-60" type="ancient-note">
      <add type="abstract">Sane uera lectio est <emph style="font-style:italic">turbatur</emph>, ut sit impersonale...si enim <emph style="font-style:italic">turbamur</emph> legeris, uidetur ad paucos referri.</add>
  </rdg>
</app>
```

In B:

```xml
<!--fr 15.3-->
<app from="#d001w94" to="#d001w94">
  <lem wit="#lw1-14" source="#lb1-50 #lb1-60 #lb1-61 #lb1-16">turbatur<note type="details" target="#lb1-50"> 1,4,28 (<emph style="font-style:italic">sed</emph> turbamur <emph style="font-style:italic">cod. Ambrosianus E</emph>. 153 <emph style="font-style:italic">sup</emph>.),</note><note type="details" target="#lb1-60"> (<emph style="font-style:italic">sed</emph> turbamur <emph style="font-style:italic">cod. Caroliruhensis</emph> 116),</note><note type="details" target="#lb1-61"> <emph style="font-style:italic">Aen</emph>. 1,272,</note><note type="details" target="#lb1-16"> V p. 372,35</note></lem>
  <rdg wit="#lw1-16 #lw1-21 #lw1-31 #lw1-27" source="#lb1-39">turbamur<ident n="d001w94">TVRBAMVR</ident></rdg>
</app>
```

The last `rdg` element of A is missing from B, because it's an ancient note. In B it is found later, under its own `app`, among the elements built from the ancient-notes layer:

```xml
<!--fr 15.3-->
<app type="ancient-note" from="#d001w94" to="#d001w94">
  <rdg type="ancient-note" source="#lb1-60">
    <add type="abstract">Sane uera lectio est <emph style="font-style:italic">turbatur</emph>, ut sit impersonale...si enim <emph style="font-style:italic">turbamur</emph> legeris, uidetur ad paucos referri.</add>
  </rdg>
</app>
```
