/*
    Remove the idea of JoinSegment! Replace with join map in Join and rework to allow for room-room openings
    Start must be "plugged"
    If dunegon ends when it's too small, new entrys should be added to working set! (if fork < 3 add 1 att appropriate ställe)
    Rollbacks should replace original fork with exit -1, not end with deadends!
    Buggar: 
        - Efter backout and removewithonelessexit, the old "tiles" are still there - 2 sets of tiles (room only?) - Happends when there are 1 or more backouts... 
        - Ibland skrivs korridorer över? (bara backedOut and replaced??)

    -- Check findpath for krocks..
    -- Set stuff as classProps in join
    -- Straight corridors in a row should lower chance of another straight..
    -- Corridor endings should be plugged with a deadend!
    -- Corridor merge should not overlay existing space with a straight - must replace adjoining section with a section exit +1 
    -- New JoinSegment - should add nessecary segments starting with a straight, to end at a predefined "join" coordinate. Coord before "join" is the new exit
    -- Rollbacked corridors should end with a deadend
    -- Buggar: 
        - Krock med annan scan backar ut ena parten, men den andra stannar halvfärdig
        - Backing out to a segment with 2 exits - need to check that there segments at the other exit, else continue backing out.. 
        - RedoSegmentWithOneLessExit segment not found... backing out of "joinsegment" 
        - Joinsegment för nära join så att addSegment börjar på sidan av join
        - Join till hörn av rum direction -x, så läggs den nya exiten till, men det gamla hörnet finns kvar och stänger exiten?
        - Fail to create exit in large room.. (-z)
        - After backout and remaderoomwith1lessexit - check that all exits are populated, else redo again! 
        - Vid en join med replaceSegmentWithNewExit, där det gamla segmentet fortfarande ligger i workingSet så sker en utbackning på den tråden, som lämnar en tarm med 2 öppningar!
*/
