# Checking Overlap Removal

This operation removes the overlaps from the apparatus XML documents, saving the results into another folder.

This removal happens by "cutting" all the children elements of the overlapped entry (except its `lem`), adding to each of them an `@n` attribute with their parent `app` original `@from`/`@to` values, and pasting them at the end of the content of the target `app` element. If the cut `lem` element had any `@wit` or `@source` attribute whose content is not a subset of the target `app`'s `lem`, an error is logged because we are going to lose some information which users should restore manually later in the editor.

## Log

The first check is looking at the operation log and examine any eventual error (look for `[ERR]`) or warning (look for `[WRN]`).

The only errors present should refer to lost sources, as explained above.

For instance, here are the first lines of a log:

```txt
2020-04-22 15:02:50.065 +02:00 [INF] REMOVE OVERLAPS
2020-04-22 15:02:50.112 +02:00 [INF] Parsing E:\Work\mqdq\ABLAB\ABLAB-epig-app.xml
2020-04-22 15:02:50.125 +02:00 [INF] Parsing E:\Work\mqdq\AEDIT\AEDIT-epig-app.xml
2020-04-22 15:02:50.129 +02:00 [INF] Parsing E:\Work\mqdq\AEGR_PER\AEGR_PER-aegr-app.xml
2020-04-22 15:02:50.187 +02:00 [INF] Merging overlapping app from="#d001w388" to="#d001w389" into from="#d001w388" to="#d001w392"
2020-04-22 15:02:50.193 +02:00 [INF] Merging overlapping app from="#d001w801" to="#d001w802" into from="#d001w801" to="#d001w803"
2020-04-22 15:02:50.193 +02:00 [ERR] Removed overlapping app lost sources at div d001: from="#d001w801" to="#d001w802"
2020-04-22 15:02:50.195 +02:00 [INF] Merging overlapping app from="#d001w928" to="#d001w928" into from="#d001w928" to="#d001w929"
2020-04-22 15:02:50.195 +02:00 [ERR] Removed overlapping app lost sources at div d001: from="#d001w928" to="#d001w928"
2020-04-22 15:02:50.196 +02:00 [INF] Merging overlapping app from="#d001w1003" to="#d001w1003" into from="#d001w1002" to="#d001w1004"
2020-04-22 15:02:50.196 +02:00 [ERR] Removed overlapping app lost sources at div d001: from="#d001w1003" to="#d001w1003"
2020-04-22 15:02:50.200 +02:00 [INF] Merging overlapping app from="#d001w1375" to="#d001w1375" into from="#d001w1375" to="#d001w1377"
2020-04-22 15:02:50.200 +02:00 [INF] Merging overlapping app from="#d001w1394" to="#d001w1394" into from="#d001w1392" to="#d001w1394"
2020-04-22 15:02:50.202 +02:00 [INF] Merging overlapping app from="#d001w1850" to="#d001w1850" into from="#d001w1848" to="#d001w1850"
2020-04-22 15:02:50.205 +02:00 [INF] Parsing E:\Work\mqdq\AEMIL\AEMIL-frag-app.xml
...
```

The log lists each apparatus file and notifies about merging or errors in overlap removal.

## Overlaps List

When requested, the remove overlaps command also outputs a file named `~overlap-err-divs.txt`, which contains a list of shortened file names and div IDs, one item per line.

This list contains the IDs of all the div's which had a lost source error during overlap removal, and can be used by the parse-text command to flag the corresponding items.

Sample:

```txt
AEGR_PER-aegr d001
ALC_AVIT-carm d005
ALC_AVIT-carm d006
ANTH-anth d004
ANTH-anth d011
...
```
