   M S B A M L   `   `   `  ����|  xSystem.Windows.Controls.DataVisualization.Toolkit, Version=3.5.40128.1, Culture=neutral, PublicKeyToken=31bf3856ad364e35�{clr-namespace:System.Windows.Controls.DataVisualization.Charting;assembly=System.Windows.Controls.DataVisualization.Toolkit2System.Windows.Controls.DataVisualization.Charting  5   �    WPFDemoServer   WPFDemoServer.Window1   5   	   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35U QWPFToolkit, Version=3.5.40128.1, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35H 9http://schemas.microsoft.com/winfx/2006/xaml/presentation      5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   �chartingToolkit{clr-namespace:System.Windows.Controls.DataVisualization.Charting;assembly=System.Windows.Controls.DataVisualization.Toolkit     5   �   -   5          � Title$  Save file dialog��5   	   $	��408��6"   $	��545��6/    � WindowStyle$ 
ToolWindow=�6;   .��5      � .+�5   
   �� -   5        $��textBox1��5   1   $��23��6   $��	12,34,0,0q�6   $	��Top=�6A   {�.  $
��Left=�6k   $	��258��6�   �� 5   
   -   5        $��label1��5   S   $��Record file path:��6   $��28��6,   $
��Left=�68   $	��Top=�6a   �� 5   
   -   5        $��button1��5   ?   $	��...��6   $��23��6   $��
276,34,0,0q�6+   $	��Top=�6N   $
��Left=�6f   $��26��6�   6�   �� 5	   
   -   5        $��button2��5	   ;   $��
Start Test��6   $��11,294,0,12q�6&   $
��Left=�6`   $	��291��6{   �� 5
   
   -   5        $��slider1��5
   K   $��23��6   $
��Left=�6   $��	12,90,0,0q�68   $	��Top=�6Z   $	��290��6r    � Minimum$	 0.5Q�6~    � Maximum$ 8Q�6�    � Value$ 1Q�6�    �� IsSnapToTickEnabled .  �� TickFrequency$	 0.5Q�6�    �� TickPlacement$ BottomRight=�6�   �� 5   
   -   5        $��label2��5   d   $��Mouse Scaling��6   $��28��6(   $
��Left=�64   $��112,119,0,0q�6O   $	��Top=�6r   �� 5   
   -   5        $��slider2��5   d   $��23��6   $
��Left=�6   $��
13,153,0,0q�68   $ 7Q�6L   $ 1Q�6X   $ 2Q�6s   $	��Top=�6}   $	��290��6�   $ BottomRight=�6�    .  � LargeChange$	 0.5Q�6�   $	 0.5Q�6  �� 5   
   -	   5        $��label3��5   d   $��Frame Scaling��6   $��28��6(   $
��Left=�64   $��112,182,0,0q�6O   $	��Top=�6r   �� 5   
   -
   5        $��	repeatBox��5   <   $��20��6   $��21��6   $��	69,63,0,0q�6)   $	��Top=�6M   $
��Left=�6e   $��56��6�   �� 5   
   -   5        $��label4��5   [   $��Repeat��6   $��28��6!   $
��Left=�6-   $��	13,61,0,0q�6H   $	��Top=�6i   �� 5   
   -   5        $��label5��5   ]   $��time(s)��6   $��28��6"   $
��Left=�6.   $��
131,61,0,0q�6I   $	��Top=�6k   �� 5   
   -   5        $��
textBlock1��5   )   $��331,19,0,12q�6   $��	TextBlock��6;   �� 5   
   -   5        $��slider3��5   d   $��23��6   $
��Left=�6   $��
13,216,0,0q�68   $ 2Q�6L   $ 1Q�6X   $ BottomRight=�6s   $	 1.5Q�6�   $	��Top=�6�   $	��290��6�   $	 0.1Q�6�   	 � SmallChange$
	 0.01Q�6�   $	 0.1Q�6�    . 6  �� 5   
   -   5        $��label6��5   f   $��Shrinking index��6   $��28��6*   $
��Left=�66   $��112,245,0,0q�6Q   $	��Top=�6t   �� 5   
   -   5        $��	checkBox1��5   _   $��Shrink��6   $��16��6$   $
��Left=�60   $��
75,272,0,0q�6K   $	��Top=�6p   
 d� 	IsChecked$

 TrueY�6�   6�   �� 5   
   -   5        $��	checkBox2��5   }   $��Mouse Controlling��6   $��16��6/   $
��Left=�6;   $
 FalseY�6V   $��137,272,0,0q�6h   $	��Top=�6�   6�   �� 5   
   -   5        $��	checkBox3��5   q   $
��Line��6   $��16��6"   $��Right=�6.   $
 FalseY�6J   $��0,272,226,0q�6\   $	��Top=�6�   6�   �� 5   
   -   5         ��Name$ 	checkBox4��5   ^   $��Frame��6   $��16��6#   $
��Left=�6/   $��
13,272,0,0q�6J   $	��Top=�6q   $

 TrueY�6�   6�   5      5      