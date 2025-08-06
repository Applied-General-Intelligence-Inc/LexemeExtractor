       IDENTIFICATION DIVISION.                                                 
                                                                                
       PROGRAM-ID.  IND2000.
                                                                                
       ENVIRONMENT DIVISION.                                                    
                                                                                
       INPUT-OUTPUT SECTION.                                                    
                                                                                
       FILE-CONTROL.                                                            
       
           SELECT MNTTRAN  ASSIGN TO MNTTRAN.
           SELECT INVMAST  ASSIGN TO INVMAST
                           ORGANIZATION IS INDEXED
                           ACCESS IS RANDOM
                           RECORD KEY IS IR-ITEM-NO.
           SELECT ERRTRAN  ASSIGN TO ERRTRAN
                           FILE STATUS IS ERRTRAN-FILE-STATUS.
                                                                                
       DATA DIVISION.                                                           
                                                                                
       FILE SECTION.                                                            
                                                                                
       FD  MNTTRAN.
                                                                                
       01  TRANSACTION-RECORD      PIC X(61).
                                                                                
       FD  INVMAST.
                                                                                
       01  INVENTORY-RECORD-AREA.
           05  IR-ITEM-NO          PIC X(5).
           05  FILLER              PIC X(65).
                                                                                
       FD  ERRTRAN.
                                                                                
       01  ERROR-TRANSACTION       PIC X(61).
                                                                                
       WORKING-STORAGE SECTION.                                                 
                                                                                
       01  SWITCHES.                                                            
           05  TRANSACTION-EOF-SWITCH  PIC X   VALUE "N".
               88  TRANSACTION-EOF             VALUE "Y".
           05  MASTER-FOUND-SWITCH     PIC X   VALUE "Y".
               88  MASTER-FOUND                VALUE "Y".
       
       01  FILE-STATUS-FIELDS.
           05  ERRTRAN-FILE-STATUS     PIC XX.
               88  ERRTRAN-SUCCESSFUL          VALUE "00".
       
       01  MAINTENANCE-TRANSACTION.
           05  MT-TRANSACTION-CODE     PIC X.
               88  DELETE-RECORD               VALUE "1".
               88  ADD-RECORD                  VALUE "2".
               88  CHANGE-RECORD               VALUE "3".
           05  MT-MASTER-DATA.
               10  MT-ITEM-NO          PIC X(5).
               10  MT-ITEM-DESC        PIC X(40).
               10  MT-UNIT-COST        PIC S9(3)V99.
               10  MT-UNIT-PRICE       PIC S9(3)V99.
               10  MT-REORDER-POINT    PIC S9(5).
                                                                                
       01  INVENTORY-MASTER-RECORD.
           05  IM-ITEM-NO              PIC X(5).
           05  IM-DESCRIPTIVE-DATA.
               10  IM-ITEM-DESC        PIC X(40).
               10  IM-UNIT-COST        PIC S9(3)V99.
               10  IM-UNIT-PRICE       PIC S9(3)V99.
           05  IM-INVENTORY-DATA.
               10  IM-REORDER-POINT    PIC S9(5).
               10  IM-ON-HAND          PIC S9(5).
               10  IM-ON-ORDER         PIC S9(5).
       
       PROCEDURE DIVISION.                                                      
                                                                                
       000-MAINTAIN-INVENTORY-FILE.
                                                                                
           OPEN INPUT  MNTTRAN
                I-O    INVMAST
                OUTPUT ERRTRAN.
           PERFORM 300-MAINTAIN-INVENTORY-RECORD
               UNTIL TRANSACTION-EOF.
           CLOSE MNTTRAN
                 INVMAST
                 ERRTRAN.
           STOP RUN.                                                            
                                                                                
       300-MAINTAIN-INVENTORY-RECORD.
                                                                                
           PERFORM 310-READ-INVENTORY-TRANSACTION.
           IF NOT TRANSACTION-EOF
               PERFORM 320-READ-INVENTORY-MASTER
               IF DELETE-RECORD
                   IF MASTER-FOUND
                       PERFORM 330-DELETE-INVENTORY-RECORD
                   ELSE
                       PERFORM 380-WRITE-ERROR-TRANSACTION
               ELSE IF ADD-RECORD
                   IF MASTER-FOUND
                       PERFORM 380-WRITE-ERROR-TRANSACTION
                   ELSE
                       PERFORM 340-ADD-INVENTORY-RECORD
               ELSE IF CHANGE-RECORD
                   IF MASTER-FOUND
                       PERFORM 360-CHANGE-INVENTORY-RECORD
                   ELSE
                       PERFORM 380-WRITE-ERROR-TRANSACTION.
       
       310-READ-INVENTORY-TRANSACTION.
       
           READ MNTTRAN INTO MAINTENANCE-TRANSACTION
               AT END
                   SET TRANSACTION-EOF TO TRUE.
       
       320-READ-INVENTORY-MASTER.
       
           MOVE MT-ITEM-NO TO IR-ITEM-NO.
           READ INVMAST INTO INVENTORY-MASTER-RECORD
               INVALID KEY
                   MOVE "N" TO MASTER-FOUND-SWITCH
               NOT INVALID KEY
                   SET MASTER-FOUND TO TRUE.
       
       330-DELETE-INVENTORY-RECORD.
       
           DELETE INVMAST.
       
       340-ADD-INVENTORY-RECORD.
       
           MOVE MT-ITEM-NO       TO IM-ITEM-NO.
           MOVE MT-ITEM-DESC     TO IM-ITEM-DESC.
           MOVE MT-UNIT-COST     TO IM-UNIT-COST.
           MOVE MT-UNIT-PRICE    TO IM-UNIT-PRICE.
           MOVE MT-REORDER-POINT TO IM-REORDER-POINT.
           MOVE ZERO             TO IM-ON-HAND.
           MOVE ZERO             TO IM-ON-ORDER.
           PERFORM 350-WRITE-INVENTORY-RECORD.
       
       350-WRITE-INVENTORY-RECORD.
       
           WRITE INVENTORY-RECORD-AREA FROM INVENTORY-MASTER-RECORD
               INVALID KEY
                   DISPLAY "WRITE ERROR ON INVMAST FOR ITEM NUMBER "
                       IR-ITEM-NO
                   SET TRANSACTION-EOF TO TRUE.
       
       360-CHANGE-INVENTORY-RECORD.
       
           IF MT-ITEM-DESC NOT = SPACE
               MOVE MT-ITEM-DESC TO IM-ITEM-DESC.
           IF MT-UNIT-COST NOT = ZERO
               MOVE MT-UNIT-COST TO IM-UNIT-COST.
           IF MT-UNIT-PRICE NOT = ZERO
               MOVE MT-UNIT-PRICE TO IM-UNIT-PRICE.
           IF MT-REORDER-POINT NOT = ZERO
               MOVE MT-REORDER-POINT TO IM-REORDER-POINT.
           PERFORM 370-REWRITE-INVENTORY-RECORD.
       
       370-REWRITE-INVENTORY-RECORD.
       
           REWRITE INVENTORY-RECORD-AREA FROM INVENTORY-MASTER-RECORD.
       
       380-WRITE-ERROR-TRANSACTION.
       
           WRITE ERROR-TRANSACTION FROM MAINTENANCE-TRANSACTION.
           IF NOT ERRTRAN-SUCCESSFUL
               DISPLAY "WRITE ERROR ON ERRTRAN FOR ITEM NUMBER "
                   MT-ITEM-NO
               DISPLAY "FILE STATUS CODE IS " ERRTRAN-FILE-STATUS
               SET TRANSACTION-EOF TO TRUE.
       
