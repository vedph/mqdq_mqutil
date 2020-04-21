# Checking Apparatus

The apparatus is exported into a copy of the original TEI documents, by clearing the content of all the `div1` elements (except for their eventual first `head` child element), and replacing it with newly generated XML code.

As explained in the [export discussion](export.md), the order of the elements in this content will vary, and in some cases (for "sparse fragments") there might not be a 1:1 correspondance.

Also, I prefer to generate comments in the exported documents, so that I can precisely refer each XML branch to its source.

So, here the best approach is probably comparing `app` with `app`, in the context of the same parent element.

## Sample

Document `VERG-eclo-app.xml`: the original document (A) is compared side by side with the updated document (B).

All what is outside `body` should be unchanged. Inside `body`, in A we first find a `div1` with some attributes, a `head` child, and a number of `app` children.

This `div1` should be found with the same attributes in B. Also, the `head` element should appear unchanged in B, as its first child element.

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

In B, we find another order, following the distribution of data into layers (I prefix each entry with the corresponding entry number in A). I include the comments in the list. These comments are inserted to mark the start of a new set of data: there is one whenever a new item starts, a new part starts, or a new fragment starts. The item is recorded with its title and ID; the part with its role ID and ID; the fragment with its location. Should you need to, you can inspect items and parts by finding their ID in the database.

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

We can now turn to the content of each entry, comparing A with B. So, for instance, the first A entry is:

```xml
<app from="#d001w9" to="#d001w13" type="margin-note">
  <lem source="#lb1-56" type="ancient-note">
      <add type="abstract"><emph style="font-style:italic">Meditaris</emph> cantas, uel <emph style="font-style:italic">melitaris</emph>, -<emph style="font-style:italic">l</emph>- pro -<emph style="font-style:italic">d</emph>-, ut idem sit tropus.</add>
    </lem>
</app>
```

The corresponding entry in B is:

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
